﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Serenity.CodeGenerator.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("RazorGenerator", "2.0.0.0")]
    public partial class EntityRow : RazorGenerator.Templating.RazorTemplateBase
    {
#line hidden

        #line 2 "..\..\Views\EntityRow.cshtml"
 public dynamic Model { get; set; } 
        #line default
        #line hidden

        public override void Execute()
        {


WriteLiteral("\r\n");



            
            #line 2 "..\..\Views\EntityRow.cshtml"
                                                   
    var dotModule = Model.Module == null ? "" : ("." + Model.Module);
    var moduleDot = Model.Module == null ? "" : (Model.Module + ".");
    var schemaDot = Model.Schema == null ? "" : ("[" + Model.Schema + "].");
     
    Func<string, string, string> jf = (x, y) =>
    {
        if (x.ToLowerInvariant() == y.ToLowerInvariant())
            return y;
        else
            return x + y;
    };


            
            #line default
            #line hidden
WriteLiteral("namespace ");


            
            #line 15 "..\..\Views\EntityRow.cshtml"
      Write(Model.RootNamespace);

            
            #line default
            #line hidden

            
            #line 15 "..\..\Views\EntityRow.cshtml"
                            Write(dotModule);

            
            #line default
            #line hidden
WriteLiteral(@".Entities
{
    using Newtonsoft.Json;
    using Serenity;
    using Serenity.ComponentModel;
    using Serenity.Data;
    using Serenity.Data.Mapping;
    using System;
    using System.ComponentModel;
    using System.IO;

    [ConnectionKey(""");


            
            #line 26 "..\..\Views\EntityRow.cshtml"
               Write(Model.ConnectionKey);

            
            #line default
            #line hidden
WriteLiteral("\"), DisplayName(\"");


            
            #line 26 "..\..\Views\EntityRow.cshtml"
                                                    Write(Model.Tablename);

            
            #line default
            #line hidden
WriteLiteral("\"), InstanceName(\"");


            
            #line 26 "..\..\Views\EntityRow.cshtml"
                                                                                      Write(Model.Tablename);

            
            #line default
            #line hidden
WriteLiteral("\"), TwoLevelCached]\r\n    [ReadPermission(\"");


            
            #line 27 "..\..\Views\EntityRow.cshtml"
                Write(Model.Permission);

            
            #line default
            #line hidden
WriteLiteral("\")]\r\n    [ModifyPermission(\"");


            
            #line 28 "..\..\Views\EntityRow.cshtml"
                  Write(Model.Permission);

            
            #line default
            #line hidden
WriteLiteral("\")]\r\n    [JsonConverter(typeof(JsonRowConverter))]\r\n    public sealed class ");


            
            #line 30 "..\..\Views\EntityRow.cshtml"
                   Write(Model.RowClassName);

            
            #line default
            #line hidden
WriteLiteral(" : ");


            
            #line 30 "..\..\Views\EntityRow.cshtml"
                                         Write(Model.RowBaseClass);

            
            #line default
            #line hidden
WriteLiteral(", IIdRow");


            
            #line 30 "..\..\Views\EntityRow.cshtml"
                                                                     Write(Model.IsLookup ? ", IDbLookupRow" : "");

            
            #line default
            #line hidden

            
            #line 30 "..\..\Views\EntityRow.cshtml"
                                                                                                              Write(Model.NameField == null ? "" : ", INameRow");

            
            #line default
            #line hidden

            
            #line 30 "..\..\Views\EntityRow.cshtml"
                                                                                                                                                                WriteLiteral("\r\n    {");

            
            #line default
            #line hidden
            
            #line 31 "..\..\Views\EntityRow.cshtml"
      foreach (var x in Model.Fields) {
    var attrs = new List<string>();
    attrs.Add("DisplayName(\"" + x.Title + "\")");

    if (x.Ident != x.Name)
    {
        attrs.Add("Column(\"" + x.Name + "\")");
    }

    if ((x.Size ?? 0) != 0) {
        attrs.Add("Size(" + x.Size + ")");
    }
    if (x.Scale != 0) {
        attrs.Add("Scale(" + x.Scale + ")");
    }
    if (!String.IsNullOrEmpty(x.Flags)) {
        attrs.Add(x.Flags);
    }       
    if (!String.IsNullOrEmpty(x.PKTable)) {
        attrs.Add("ForeignKey(\"" + (string.IsNullOrEmpty(x.PKSchema) ? "" : ("[" + x.PKSchema + "].")) + x.PKTable + "\", \"" + x.PKColumn + "\")");
        attrs.Add("LeftJoin(\"j" + x.ForeignJoinAlias + "\")");
    }
    if (Model.NameField == x.Ident) {
        attrs.Add("QuickSearch");
    }
    var attrString = String.Join(", ", attrs.ToArray());

            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 58 "..\..\Views\EntityRow.cshtml"
 if (!String.IsNullOrEmpty(attrString)) {

            
            #line default
            #line hidden
WriteLiteral("        [");


            
            #line 59 "..\..\Views\EntityRow.cshtml"
          Write(attrString);

            
            #line default
            #line hidden
WriteLiteral("]\r\n");


            
            #line 60 "..\..\Views\EntityRow.cshtml"
       }
            
            #line default
            #line hidden
WriteLiteral("        public ");


            
            #line 60 "..\..\Views\EntityRow.cshtml"
                  Write(x.Type);

            
            #line default
            #line hidden

            
            #line 60 "..\..\Views\EntityRow.cshtml"
                          Write(x.IsValueType ? "?" : "");

            
            #line default
            #line hidden
WriteLiteral(" ");


            
            #line 60 "..\..\Views\EntityRow.cshtml"
                                                     Write(x.Ident);

            
            #line default
            #line hidden
WriteLiteral("\r\n        {\r\n            get { return Fields.");


            
            #line 62 "..\..\Views\EntityRow.cshtml"
                            Write(x.Ident);

            
            #line default
            #line hidden
WriteLiteral("[this]; }\r\n            set { Fields.");


            
            #line 63 "..\..\Views\EntityRow.cshtml"
                     Write(x.Ident);

            
            #line default
            #line hidden
WriteLiteral("[this] = value; }\r\n        }\r\n");


            
            #line 65 "..\..\Views\EntityRow.cshtml"
       }

            
            #line default
            #line hidden

            
            #line 66 "..\..\Views\EntityRow.cshtml"
 foreach (var x in Model.Joins){foreach (var y in x.Fields){

            
            #line default
            #line hidden
WriteLiteral("\r\n        [DisplayName(\"");


            
            #line 68 "..\..\Views\EntityRow.cshtml"
                 Write(y.Title);

            
            #line default
            #line hidden
WriteLiteral("\"), Expression(\"");


            
            #line 68 "..\..\Views\EntityRow.cshtml"
                                          Write("j" + x.Name + "." + y.Name);

            
            #line default
            #line hidden
WriteLiteral("\")]\r\n        public ");


            
            #line 69 "..\..\Views\EntityRow.cshtml"
          Write(y.Type);

            
            #line default
            #line hidden

            
            #line 69 "..\..\Views\EntityRow.cshtml"
                  Write(y.IsValueType ? "?" : "");

            
            #line default
            #line hidden
WriteLiteral(" ");


            
            #line 69 "..\..\Views\EntityRow.cshtml"
                                              Write(jf(x.Name, y.Ident));

            
            #line default
            #line hidden
WriteLiteral("\r\n        {\r\n            get { return Fields.");


            
            #line 71 "..\..\Views\EntityRow.cshtml"
                            Write(jf(x.Name, y.Ident));

            
            #line default
            #line hidden
WriteLiteral("[this]; }\r\n            set { Fields.");


            
            #line 72 "..\..\Views\EntityRow.cshtml"
                     Write(jf(x.Name, y.Ident));

            
            #line default
            #line hidden
WriteLiteral("[this] = value; }\r\n        }\r\n");


            
            #line 74 "..\..\Views\EntityRow.cshtml"
       }}

            
            #line default
            #line hidden
WriteLiteral("\r\n        IIdField IIdRow.IdField\r\n        {\r\n            get { return Fields.");


            
            #line 78 "..\..\Views\EntityRow.cshtml"
                            Write(Model.Identity);

            
            #line default
            #line hidden
WriteLiteral("; }\r\n        }\r\n");


            
            #line 80 "..\..\Views\EntityRow.cshtml"
 if (Model.NameField != null) {

            
            #line default
            #line hidden
WriteLiteral("\r\n        StringField INameRow.NameField\r\n        {\r\n            get { return Fie" +
"lds.");


            
            #line 84 "..\..\Views\EntityRow.cshtml"
                           Write(Model.NameField);

            
            #line default
            #line hidden
WriteLiteral("; }\r\n        }\r\n");


            
            #line 86 "..\..\Views\EntityRow.cshtml"
       }

            
            #line default
            #line hidden
WriteLiteral("\r\n        public static readonly RowFields Fields = new RowFields().Init();\r\n\r\n  " +
"      public ");


            
            #line 90 "..\..\Views\EntityRow.cshtml"
           Write(Model.RowClassName);

            
            #line default
            #line hidden
WriteLiteral("()\r\n            : base(Fields)\r\n        {\r\n        }\r\n\r\n        public class RowF" +
"ields : ");


            
            #line 95 "..\..\Views\EntityRow.cshtml"
                             Write(Model.FieldsBaseClass);

            
            #line default
            #line hidden

            
            #line 95 "..\..\Views\EntityRow.cshtml"
                                                         WriteLiteral("\r\n        {");

            
            #line default
            #line hidden
            
            #line 96 "..\..\Views\EntityRow.cshtml"
          foreach (var x in Model.Fields) {

            
            #line default
            #line hidden
WriteLiteral("\r\n            public readonly ");


            
            #line 98 "..\..\Views\EntityRow.cshtml"
                        Write(x.Type);

            
            #line default
            #line hidden
WriteLiteral("Field ");


            
            #line 98 "..\..\Views\EntityRow.cshtml"
                                       Write(x.Ident);

            
            #line default
            #line hidden
WriteLiteral(";");


            
            #line 98 "..\..\Views\EntityRow.cshtml"
                                                             }

            
            #line default
            #line hidden

            
            #line 99 "..\..\Views\EntityRow.cshtml"
 foreach (var x in Model.Joins) {
            
            #line default
            #line hidden
WriteLiteral("\r\n");


            
            #line 100 "..\..\Views\EntityRow.cshtml"
       foreach (var y in x.Fields) {

            
            #line default
            #line hidden
WriteLiteral("\r\n            public readonly ");


            
            #line 102 "..\..\Views\EntityRow.cshtml"
                        Write(y.Type);

            
            #line default
            #line hidden
WriteLiteral("Field ");


            
            #line 102 "..\..\Views\EntityRow.cshtml"
                                       Write(jf(x.Name, y.Ident));

            
            #line default
            #line hidden
WriteLiteral(";");


            
            #line 102 "..\..\Views\EntityRow.cshtml"
                                                                         }}

            
            #line default
            #line hidden
WriteLiteral("\r\n\r\n            public RowFields()\r\n                : base(\"");


            
            #line 106 "..\..\Views\EntityRow.cshtml"
                    Write(schemaDot);

            
            #line default
            #line hidden

            
            #line 106 "..\..\Views\EntityRow.cshtml"
                               Write(Model.Tablename);

            
            #line default
            #line hidden
WriteLiteral("\"");


            
            #line 106 "..\..\Views\EntityRow.cshtml"
                                                 Write(string.IsNullOrEmpty(Model.FieldPrefix) ? "" : (", \"" + Model.FieldPrefix + "\""));

            
            #line default
            #line hidden
WriteLiteral(")\r\n            {\r\n                LocalTextPrefix = \"");


            
            #line 108 "..\..\Views\EntityRow.cshtml"
                               Write(moduleDot);

            
            #line default
            #line hidden

            
            #line 108 "..\..\Views\EntityRow.cshtml"
                                           Write(Model.ClassName);

            
            #line default
            #line hidden
WriteLiteral("\";\r\n            }\r\n        }\r\n    }\r\n}");


        }
    }
}
#pragma warning restore 1591
