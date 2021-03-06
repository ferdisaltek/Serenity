using Serenity.Data;
using Serenity.Data.Mapping;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Serenity.Services
{
    public class SaveRequestHandler<TRow, TSaveRequest, TSaveResponse> : ISaveRequestHandler
        where TRow : Row, IIdRow, new()
        where TSaveResponse : SaveResponse, new()
        where TSaveRequest : SaveRequest<TRow>, new()
    {
        private bool displayOrderFix;
        private static bool loggingInitialized;
        protected static CaptureLogHandler<TRow> captureLogHandler;
        protected static bool hasAuditLogAttribute;
        protected IEnumerable<SaveRequestBehaviourAttribute> behaviours;

        public SaveRequestHandler()
        {
            this.behaviours = this.GetType().GetCustomAttributes<SaveRequestBehaviourAttribute>();
        }

        protected virtual void AfterSave()
        {
            HandleDisplayOrder(afterSave: true);

            foreach (var behaviour in this.behaviours)
                behaviour.OnAfterSave(this);
        }

        protected virtual void BeforeSave()
        {
            foreach (var behaviour in this.behaviours)
                behaviour.OnBeforeSave(this);
        }

        protected virtual void ClearNonTableAssignments()
        {
            foreach (var field in Row.GetFields())
            {
                if (Row.IsAssigned(field) && !field.IsTableField())
                    Row.ClearAssignment(field);
            }
        }

        protected virtual void PerformAuditing()
        {
            if (!loggingInitialized)
            {
                var logTableAttr = typeof(TRow).GetCustomAttribute<CaptureLogAttribute>();
                if (logTableAttr != null)
                    captureLogHandler = new CaptureLogHandler<TRow>();

                hasAuditLogAttribute = typeof(TRow).GetCustomAttribute<AuditLogAttribute>(false) != null;

                loggingInitialized = true;
            }

            if (captureLogHandler != null)
                DoCaptureLog();
            else if (hasAuditLogAttribute)
                DoGenericAudit();
        }

        protected virtual void DoCaptureLog()
        {
            if (IsUpdate)
            {
                var logRow = Row as ILoggingRow;
                bool anyChanged = false;
                foreach (var field in this.Row.GetTableFields())
                {
                    if (logRow != null &&
                        (ReferenceEquals(logRow.InsertDateField, field) ||
                         ReferenceEquals(logRow.UpdateDateField, field) ||
                         ReferenceEquals(logRow.InsertUserIdField, field) ||
                         ReferenceEquals(logRow.UpdateUserIdField, field)))
                    {
                        continue;
                    }

                    if (field.IndexCompare(Old, Row) != 0)
                    {
                        anyChanged = true;
                        break;
                    }
                }

                if (anyChanged)
                    captureLogHandler.Log(this.UnitOfWork, this.Row, Authorization.UserId.TryParseID().Value, isDelete: false);
            }
            else if (IsCreate)
            {
                captureLogHandler.Log(this.UnitOfWork, this.Row, Authorization.UserId.TryParseID().Value, isDelete: false);
            }
        }

        protected virtual void DoGenericAudit()
        {
            var auditFields = new HashSet<Field>();
            GetEditableFields(auditFields);

            var auditRequest = GetAuditRequest(auditFields);

            if (IsUpdate)
            {
                Row audit = null;
                if (auditRequest != null)
                    audit = AuditLogService.PrepareAuditUpdate(RowRegistry.GetConnectionKey(Row), auditRequest);

                if (audit != null)
                    Connection.Insert(audit);
            }
            else if (IsCreate)
            {
                if (auditRequest != null)
                    AuditLogService.AuditInsert(Connection, RowRegistry.GetConnectionKey(Row), auditRequest);
            }
        }

        protected virtual void ExecuteSave()
        {
            if (IsUpdate)
            {
                if (Row.IsAnyFieldAssigned)
                {
                    Connection.UpdateById(Row);
                    InvalidateCacheOnCommit();
                }
            }
            else if (IsCreate)
            {
                var idField = Row.IdField as Field;
                if (!ReferenceEquals(null, idField) &&
                    idField.Flags.HasFlag(FieldFlags.AutoIncrement))
                {
                    Response.EntityId = Connection.InsertAndGetID(Row);
                    Row.IdField[Row] = Response.EntityId;
                }
                else
                {
                    Connection.Insert(Row);
                    if (!ReferenceEquals(null, idField))
                        Response.EntityId = Row.IdField[Row];
                }

                InvalidateCacheOnCommit();
            }
        }

        protected virtual AuditSaveRequest GetAuditRequest(HashSet<Field> auditFields)
        {
            Field[] array = new Field[auditFields.Count];
            auditFields.CopyTo(array);

            var auditRequest = new AuditSaveRequest(Row.Table, (IIdRow)Old, (IIdRow)Row, array);

            var parentIdRow = Row as IParentIdRow;
            if (parentIdRow != null)
            {
                var parentIdField = (Field)parentIdRow.ParentIdField;

                if (!parentIdField.ForeignTable.IsTrimmedEmpty())
                {
                    auditRequest.ParentTypeId = parentIdField.ForeignTable;
                    auditRequest.OldParentId = Old == null ? null : parentIdRow.ParentIdField[Old];
                    auditRequest.NewParentId = parentIdRow.ParentIdField[Row];
                }
            }

            return auditRequest;
        }

        protected virtual BaseCriteria GetDisplayOrderFilter()
        {
            return DisplayOrderFilterHelper.GetDisplayOrderFilterFor(Row);
        }

        protected virtual void GetEditableFields(HashSet<Field> editable)
        {
            var flag = IsCreate ? FieldFlags.Insertable : FieldFlags.Updatable;

            foreach (var field in Row.GetFields())
                if (field.Flags.HasFlag(flag))
                    editable.Add(field);
        }

        protected virtual void GetRequiredFields(HashSet<Field> required, HashSet<Field> editable)
        {
            foreach (var field in Row.GetFields())
            {
                if (editable.Contains(field) &&
                    (field.Flags & FieldFlags.NotNull) == FieldFlags.NotNull &
                    (field.Flags & FieldFlags.TrimToEmpty) != FieldFlags.TrimToEmpty)
                {
                    required.Add(field);
                }
            }
        }

        protected virtual void HandleDisplayOrder(bool afterSave)
        {
            var displayOrderRow = Row as IDisplayOrderRow;
            if (displayOrderRow == null)
                return;

            if (IsCreate && !afterSave)
            {
                var value = displayOrderRow.DisplayOrderField.AsObject(Row);
                if (value == null || Convert.ToInt32(value) <= 0)
                {
                    var filter = GetDisplayOrderFilter();
                    displayOrderRow.DisplayOrderField.AsObject(Row,
                        DisplayOrderHelper.GetNextValue(Connection, displayOrderRow, filter));
                }
                else
                    displayOrderFix = true;
            }
            else if (afterSave && 
                ((IsCreate && displayOrderFix) || 
                 (IsUpdate && displayOrderRow.DisplayOrderField[Old] != displayOrderRow.DisplayOrderField[Row])))
            {
                DisplayOrderHelper.ReorderValues(
                    connection: Connection,
                    row: displayOrderRow,
                    filter: GetDisplayOrderFilter(),
                    recordID: Row.IdField[Row].Value,
                    newDisplayOrder: displayOrderRow.DisplayOrderField[Row].Value,
                    hasUniqueConstraint: false);
            }
        }

        protected virtual void HandleNonEditable(Field field)
        {
            if (IsUpdate && field.IndexCompare(Row, Old) == 0)
            {
                field.CopyNoAssignment(Old, Row);
                Row.ClearAssignment(field);
                return;
            }

            bool isNonTableField = ((field.Flags & FieldFlags.Foreign) == FieldFlags.Foreign) ||
                  ((field.Flags & FieldFlags.Calculated) == FieldFlags.Calculated) ||
                  ((field.Flags & FieldFlags.ClientSide) == FieldFlags.ClientSide);

            if (IsUpdate)
            {
                if ((field.Flags & FieldFlags.Reflective) != FieldFlags.Reflective)
                {
                    if (!isNonTableField)
                        throw DataValidation.ReadOnlyError(Row, field);

                    field.CopyNoAssignment(Old, Row);
                    Row.ClearAssignment(field);
                }
            }
            else if (IsCreate)
            {
                if (!field.IsNull(Row) &&
                    (field.Flags & FieldFlags.Reflective) != FieldFlags.Reflective)
                {
                    if (!isNonTableField)
                        throw DataValidation.ReadOnlyError(Row, field);

                    field.AsObject(Row, null);
                    Row.ClearAssignment(field);
                }
            }

            if (Row.IsAssigned(field))
                Row.ClearAssignment(field);
        }

        protected virtual void LoadOldEntity()
        {
            var idField = (Field)(Row.IdField);
            var id = Row.IdField[Row].Value;

            if (!PrepareQuery().GetFirst(Connection))
                throw DataValidation.EntityNotFoundError(Row, id);
        }

        protected virtual void OnReturn()
        {
            foreach (var behaviour in this.behaviours)
                behaviour.OnReturn(this);
        }

        protected virtual SqlQuery PrepareQuery()
        {
            var idField = (Field)(Row.IdField);
            var id = Row.IdField[Row].Value;

            return new SqlQuery()
                .From(Old)
                .SelectTableFields()
                .WhereEqual(idField, id);
        }

        public TSaveResponse Process(IUnitOfWork unitOfWork, TSaveRequest request,
            SaveRequestType requestType = SaveRequestType.Auto)
        {
            if (unitOfWork == null)
                throw new ArgumentNullException("unitOfWork");

            UnitOfWork = unitOfWork;

            Request = request;
            Response = new TSaveResponse();

            Row = request.Entity;
            if (Row == null)
                throw new ArgumentNullException("Entity");

            if (requestType == SaveRequestType.Auto)
            {
                if (Row.IdField[Row] == null)
                    requestType = SaveRequestType.Create;
                else
                    requestType = SaveRequestType.Update;
            }

            if (requestType == SaveRequestType.Update)
            {
                ValidateAndClearIdField();
                Old = new TRow();
                LoadOldEntity();
            }

            ValidateRequest();
            SetInternalFields();
            BeforeSave();

            ClearNonTableAssignments();
            ExecuteSave();

            AfterSave();

            PerformAuditing();

            OnReturn();
            return Response;
        }

        protected virtual void SetDefaultValue(Field field)
        {
            if (field.DefaultValue == null)
                return;

            field.AsObject(Row, field.ConvertValue(field.DefaultValue, CultureInfo.InvariantCulture));
        }

        protected virtual void SetDefaultValues()
        {
            foreach (var field in Row.GetTableFields())
            {
                if (Row.IsAssigned(field) || !field.IsNull(Row))
                    continue;

                SetDefaultValue(field);
            }

            var isActiveRow = Row as IIsActiveRow;
            if (isActiveRow != null &&
                !Row.IsAssigned(isActiveRow.IsActiveField))
                isActiveRow.IsActiveField[Row] = 1;
        }

        protected virtual void SetInternalFields()
        {
            if (IsCreate)
            {
                HandleDisplayOrder(afterSave: false);
                SetTrimToEmptyFields();
                SetDefaultValues();
            }

            SetInternalLogFields();

            foreach (var behaviour in this.behaviours)
                behaviour.OnSetInternalFields(this);
        }

        protected virtual void SetInternalLogFields()
        {
            var updateLogRow = Row as IUpdateLogRow;
            var insertLogRow = Row as IInsertLogRow;

            if (updateLogRow != null && (IsUpdate || insertLogRow == null))
            {
                updateLogRow.UpdateDateField[Row] = DateTimeField.ToDateTimeKind(DateTime.Now, updateLogRow.UpdateDateField.DateTimeKind);
                updateLogRow.UpdateUserIdField[Row] = Authorization.UserId.TryParseID();
            }
            else if (insertLogRow != null && IsCreate)
            {
                insertLogRow.InsertDateField[Row] = DateTimeField.ToDateTimeKind(DateTime.Now, insertLogRow.InsertDateField.DateTimeKind);
                insertLogRow.InsertUserIdField[Row] = Authorization.UserId.TryParseID();
            }
        }

        protected virtual void SetTrimToEmptyFields()
        {
            foreach (var field in Row.GetFields())
                if (!Row.IsAssigned(field) &&
                    (field is StringField &&
                    (field.Flags & FieldFlags.Insertable) == FieldFlags.Insertable &
                    (field.Flags & FieldFlags.NotNull) == FieldFlags.NotNull &
                    (field.Flags & FieldFlags.TrimToEmpty) == FieldFlags.TrimToEmpty))
                {
                    ((StringField)field)[Row] = "";
                }
        }

        protected virtual void ValidateEditableFields(HashSet<Field> editable)
        {
            foreach (Field field in Row.GetFields())
            {
                if (IsUpdate && !Row.IsAssigned(field))
                {
                    field.CopyNoAssignment(Old, Row);
                    Row.ClearAssignment(field);
                    continue;
                }

                var stringField = field as StringField;
                if (!ReferenceEquals(null, stringField) &&
                    Row.IsAssigned(field) &&
                    (field.Flags & FieldFlags.Trim) == FieldFlags.Trim)
                {
                    string value = stringField[Row];

                    if ((field.Flags & FieldFlags.TrimToEmpty) == FieldFlags.TrimToEmpty)
                        value = value.TrimToEmpty();
                    else // TrimToNull
                        value = value.TrimToNull();

                    stringField[Row] = value;
                }

                if (!editable.Contains(field))
                    HandleNonEditable(field);
            }
        }

        protected virtual HashSet<Field> ValidateEditable()
        {
            var editableFields = new HashSet<Field>();
            GetEditableFields(editableFields);
            ValidateEditableFields(editableFields);
            return editableFields;
        }

        protected virtual void ValidateRequired(HashSet<Field> editableFields)
        {
            var requiredFields = new HashSet<Field>();
            GetRequiredFields(required: requiredFields, editable: editableFields);

            if (IsUpdate)
                Row.ValidateRequiredIfModified(requiredFields);
            else
                Row.ValidateRequired(requiredFields);
        }

        protected virtual void ValidateRequest()
        {
            ValidatePermissions();

            var editableFields = ValidateEditable();
            ValidateRequired(editableFields);

            if (IsUpdate)
                ValidateIsActive();

            ValidateFieldValues();

            ValidateUniqueConstraints();

            foreach (var behaviour in this.behaviours)
                behaviour.OnValidateRequest(this);
        }

        protected virtual void ValidateFieldValues()
        {
            var context = new RowValidationContext(this.Connection, this.Row);

            foreach (var field in Row.GetFields())
            {
                if (!Row.IsAssigned(field))
                    continue;

                if (field.CustomAttributes == null)
                    continue;

                var validators = field.CustomAttributes.OfType<ICustomValidator>();
                foreach (var validator in validators)
                {
                    context.Value = field.AsObject(this.Row);

                    var error = CustomValidate(context, field, validator);

                    if (error != null)
                        throw new ValidationError("CustomValidationError", field.PropertyName ?? field.Name, error);
                }
            }
        }

        protected virtual void ValidateUniqueConstraints()
        {
            foreach (var field in Row.GetTableFields())
            {
                if (!field.Flags.HasFlag(FieldFlags.Unique))
                    continue;

                var attr = field.CustomAttributes.OfType<UniqueAttribute>().FirstOrDefault();
                if (attr != null && !attr.CheckBeforeSave)
                    continue;

                ValidateUniqueConstraint(new Field[] { field }, 
                    attr == null ? (string)null : attr.ErrorMessage, 
                    Criteria.Empty);
            }

            foreach (var attr in typeof(TRow).GetCustomAttributes<UniqueConstraintAttribute>())
            {
                if (!attr.CheckBeforeSave)
                    continue;

                ValidateUniqueConstraint(attr.Fields.Select(x =>
                {
                    var field = Row.FindFieldByPropertyName(x) ?? Row.FindField(x);
                    if (ReferenceEquals(null, field))
                    {
                        throw new InvalidOperationException(String.Format(
                            "Can't find field '{0}' of unique constraint in row type '{1}'",
                                x, typeof(TRow).FullName));
                    }
                    return field;
                }), attr.ErrorMessage, Criteria.Empty);
            }
        }

        protected virtual void ValidateUniqueConstraint(IEnumerable<Field> fields, 
            string errorMessage = null, BaseCriteria groupCriteria = null)
        {
            if (IsUpdate && !fields.Any(x => x.IndexCompare(Old, Row) != 0))
                return;

            var criteria = groupCriteria ?? Criteria.Empty;
                   
            foreach (var field in fields)
                if (field.IsNull(Row))
                    criteria &= field.IsNull();
                else
                    criteria &= field == new ValueCriteria(field.AsObject(Row));

            if (IsUpdate)
                criteria &= (Field)Row.IdField != Row.IdField[Old].Value;

            if (Connection.Exists<TRow>(criteria))
            {
                throw new ValidationError("UniqueViolation", 
                    String.Join(", ", fields.Select(x => x.PropertyName ?? x.Name)),
                    String.Format(!string.IsNullOrEmpty(errorMessage) ? 
                        (LocalText.TryGet(errorMessage) ?? errorMessage) :
                            LocalText.Get("Validation.UniqueViolation"),
                        String.Join(", ", fields.Select(x => x.Title))));
            }
        }

        protected virtual string CustomValidate(RowValidationContext context, Field field, ICustomValidator validator)
        {
            return validator.Validate(context);
        }

        protected virtual void ValidateIsActive()
        {
            var isActiveRow = Old as IIsActiveRow;
            if (isActiveRow != null &&
                isActiveRow.IsActiveField[Old] < 0)
                throw DataValidation.RecordNotActive(Old);
        }

        protected virtual void ValidateAndClearIdField()
        {
            var idField = (Field)(Row.IdField);
            Row.ValidateRequired(idField);
            Row.ClearAssignment(idField);
        }

        protected virtual void ValidatePermissions()
        {
            PermissionAttributeBase attr = null;
            
            if (IsUpdate) 
            {
                typeof(TRow).GetCustomAttribute<UpdatePermissionAttribute>(false);
            }
            else if (IsCreate)
            {
                typeof(TRow).GetCustomAttribute<InsertPermissionAttribute>(false);
            }

            attr = attr ?? typeof(TRow).GetCustomAttribute<ModifyPermissionAttribute>(false);

            if (attr != null)
            {
                if (attr.Permission.IsNullOrEmpty())
                    Authorization.ValidateLoggedIn();
                else
                    Authorization.ValidatePermission(attr.Permission);
            }
        }

        protected virtual void InvalidateCacheOnCommit()
        {
            var attr = typeof(TRow).GetCustomAttribute<TwoLevelCachedAttribute>(false);
            if (attr != null)
            {
                BatchGenerationUpdater.OnCommit(this.UnitOfWork, Row.GetFields().GenerationKey);
                foreach (var key in attr.GenerationKeys)
                    BatchGenerationUpdater.OnCommit(this.UnitOfWork, key);
            }
        }

        protected IDbConnection Connection { get { return UnitOfWork.Connection; } }

        public IUnitOfWork UnitOfWork { get; protected set; }       

        public TRow Old { get; protected set; }
        
        public TRow Row { get; protected set; }

        public bool IsCreate { get { return Old == null; } }

        public bool IsUpdate { get { return Old != null; } }

        public TSaveRequest Request { get; protected set; }

        public TSaveResponse Response { get; protected set; }

        ISaveRequest ISaveRequestHandler.Request { get { return this.Request; } }

        SaveResponse ISaveRequestHandler.Response { get { return this.Response; } }

        Row ISaveRequestHandler.Old { get { return this.Old; } }

        Row ISaveRequestHandler.Row { get { return this.Row; } }
    }

    public class SaveRequestHandler<TRow> : SaveRequestHandler<TRow, SaveRequest<TRow>, SaveResponse>
        where TRow : Row, IIdRow, new()
    {
    }
}