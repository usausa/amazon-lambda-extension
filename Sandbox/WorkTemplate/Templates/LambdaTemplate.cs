// ------------------------------------------------------------------------------
// <auto-generated>
//     このコードはツールによって生成されました。
//     ランタイム バージョン: 17.0.0.0
//  
//     このファイルへの変更は、正しくない動作の原因になる可能性があり、
//     コードが再生成されると失われます。
// </auto-generated>
// ------------------------------------------------------------------------------
namespace WorkTemplate.Templates
{
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using WorkTemplate.Helpers;
    using WorkTemplate.Models;
    using System;
    
    /// <summary>
    /// Class to produce the template output
    /// </summary>
    
    #line 1 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public partial class LambdaTemplate : LambdaTemplateBase
    {
#line hidden
        /// <summary>
        /// Create the template output
        /// </summary>
        public virtual string TransformText()
        {
            this.Write("namespace ");
            
            #line 8 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(lambda.ContainingNamespace));
            
            #line default
            #line hidden
            this.Write("\r\n{\r\n    public sealed class ");
            
            #line 10 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(method.WrapperClassName));
            
            #line default
            #line hidden
            this.Write("\r\n    {\r\n");
            
            #line 12 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (lambda.ServiceLocator is not null) { 
            
            #line default
            #line hidden
            this.Write("        private readonly ");
            
            #line 13 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(lambda.ServiceLocator.FullName));
            
            #line default
            #line hidden
            this.Write(" serviceLocator;\r\n");
            
            #line 14 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\r\n");
            
            #line 16 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (method.HasBodyParameter) { 
            
            #line default
            #line hidden
            this.Write("        private readonly WorkTemplate.Serializer.IBodySerializer serializer;\r\n\r\n");
            
            #line 19 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("        private readonly ");
            
            #line 20 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(lambda.Function.FullName));
            
            #line default
            #line hidden
            this.Write(" function;\r\n\r\n        public ");
            
            #line 22 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(method.WrapperClassName));
            
            #line default
            #line hidden
            this.Write("()\r\n        {\r\n");
            
            #line 24 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (lambda.ServiceLocator is not null) { 
            
            #line default
            #line hidden
            this.Write("            serviceLocator = new ");
            
            #line 25 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(lambda.ServiceLocator.FullName));
            
            #line default
            #line hidden
            this.Write("();\r\n");
            
            #line 26 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            
            #line 27 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (method.HasBodyParameter) { 
            
            #line default
            #line hidden
            this.Write("            serializer = serviceLocator.");
            
            #line 28 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(lambda.FindSerializer()));
            
            #line default
            #line hidden
            this.Write(" ?? WorkTemplate.Serializer.JsonBodySerializer.Default;\r\n");
            
            #line 29 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("            function = new ");
            
            #line 30 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(lambda.Function.FullName));
            
            #line default
            #line hidden
            this.Write("(");
            
            #line 30 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(String.Join(",", lambda.ConstructorParameters.Select(x => "serializer." + lambda.FindService(x)))));
            
            #line default
            #line hidden
            this.Write(");\r\n        }\r\n\r\n");
            
            #line 33 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (method.IsAsync) { 
            
            #line default
            #line hidden
            this.Write("        public async System.Threding.ValueTask<Amazon.Lambda.APIGatewayEvents.API" +
                    "GatewayProxyResponse> Handle(Amazon.Lambda.APIGatewayEvents.APIGatewayProxyReque" +
                    "st request, Amazon.Lambda.Core.ILambdaContext context)\r\n");
            
            #line 35 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } else { 
            
            #line default
            #line hidden
            this.Write("        public Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse Handle(Amaz" +
                    "on.Lambda.APIGatewayEvents.APIGatewayProxyRequest request, Amazon.Lambda.Core.IL" +
                    "ambdaContext context)\r\n");
            
            #line 37 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("        {\r\n            if (request.Headers.ContainsKey(\"X-Lambda-Keep-Alive\"))\r\n " +
                    "           {\r\n                return new Amazon.Lambda.APIGatewayEvents.APIGatew" +
                    "ayProxyResponse { StatusCode = 200 };\r\n            }\r\n\r\n            try\r\n       " +
                    "     {\r\n");
            
            #line 46 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (method.HasValidationParameter) { 
            
            #line default
            #line hidden
            this.Write("                var validationErrors = new System.Collections.Generic.List<string" +
                    ">();\r\n");
            
            #line 48 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\r\n");
            
            #line 50 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 for (var i = 0; i < method.Parameters.Length; i++) { 
            
            #line default
            #line hidden
            
            #line 51 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 var parameter = method.Parameters[i]; 
            
            #line default
            #line hidden
            
            #line 52 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (parameter.ParameterType == ParameterType.FromBody) { 
            
            #line default
            #line hidden
            this.Write("                ");
            
            #line 53 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(parameter.Type.FullName));
            
            #line default
            #line hidden
            this.Write(" p");
            
            #line 53 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(i));
            
            #line default
            #line hidden
            this.Write(";\r\n                try\r\n                {\r\n                    p");
            
            #line 56 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(i));
            
            #line default
            #line hidden
            this.Write(" = serializer.Deserialize(request.Body);\r\n                }\r\n                catc" +
                    "h (System.Exception e)\r\n                {\r\n                    validationErrors." +
                    "Add(e.Message);\r\n                    p");
            
            #line 61 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(i));
            
            #line default
            #line hidden
            this.Write(" = default(");
            
            #line 61 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(parameter.Type.FullName));
            
            #line default
            #line hidden
            this.Write(");\r\n                }\r\n");
            
            #line 63 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } if (parameter.ParameterType == ParameterType.FromQuery) { 
            
            #line default
            #line hidden
            
            #line 64 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (parameter.Type.IsMultiType) { 
            
            #line default
            #line hidden
            this.Write("                p");
            
            #line 65 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(i));
            
            #line default
            #line hidden
            this.Write(" = BindHelper.BindValues<");
            
            #line 65 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(parameter.Type.FullName));
            
            #line default
            #line hidden
            this.Write(">(request.MultiValueQueryStringParameters, \"");
            
            #line 65 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(parameter.Name));
            
            #line default
            #line hidden
            this.Write("\", validationErrors);\r\n");
            
            #line 66 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } else { 
            
            #line default
            #line hidden
            this.Write("                p");
            
            #line 67 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(i));
            
            #line default
            #line hidden
            this.Write(" = BindHelper.BindValue<");
            
            #line 67 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(parameter.Type.FullName));
            
            #line default
            #line hidden
            this.Write(">(request.QueryStringParameters, \"");
            
            #line 67 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(parameter.Name));
            
            #line default
            #line hidden
            this.Write("\", validationErrors);\r\n");
            
            #line 68 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            
            #line 69 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } if (parameter.ParameterType == ParameterType.FromRoute) { 
            
            #line default
            #line hidden
            this.Write("                p");
            
            #line 70 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(i));
            
            #line default
            #line hidden
            this.Write(" = BindHelper.BindValue<");
            
            #line 70 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(parameter.Type.FullName));
            
            #line default
            #line hidden
            this.Write(">(request.PathParameters, \"");
            
            #line 70 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(parameter.Name));
            
            #line default
            #line hidden
            this.Write("\", validationErrors);\r\n");
            
            #line 71 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } if (parameter.ParameterType == ParameterType.FromHeader) { 
            
            #line default
            #line hidden
            
            #line 72 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (parameter.Type.IsMultiType) { 
            
            #line default
            #line hidden
            this.Write("                p");
            
            #line 73 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(i));
            
            #line default
            #line hidden
            this.Write(" = BindHelper.BindValues<");
            
            #line 73 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(parameter.Type.FullName));
            
            #line default
            #line hidden
            this.Write(">(request.MultiValueHeaders, \"");
            
            #line 73 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(parameter.Name));
            
            #line default
            #line hidden
            this.Write("\", validationErrors);\r\n");
            
            #line 74 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } else { 
            
            #line default
            #line hidden
            this.Write("                p");
            
            #line 75 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(i));
            
            #line default
            #line hidden
            this.Write(" = BindHelper.BindValue<");
            
            #line 75 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(parameter.Type.FullName));
            
            #line default
            #line hidden
            this.Write(">(request.Headers, \"");
            
            #line 75 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(parameter.Name));
            
            #line default
            #line hidden
            this.Write("\", validationErrors);\r\n");
            
            #line 76 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            
            #line 77 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } if (parameter.ParameterType == ParameterType.FromService) { 
            
            #line default
            #line hidden
            this.Write("                p");
            
            #line 78 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(i));
            
            #line default
            #line hidden
            this.Write(" = serviceLocator.");
            
            #line 78 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(lambda.FindService(parameter.Type)));
            
            #line default
            #line hidden
            this.Write("()\r\n");
            
            #line 79 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } else { 
            
            #line default
            #line hidden
            
            #line 80 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (parameter.Type.IsAPIGatewayProxyRequest()) { 
            
            #line default
            #line hidden
            this.Write("                p");
            
            #line 81 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(i));
            
            #line default
            #line hidden
            this.Write(" = request;\r\n");
            
            #line 82 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } else if (parameter.Type.IsLambdaContext()) { 
            
            #line default
            #line hidden
            this.Write("                p");
            
            #line 83 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(i));
            
            #line default
            #line hidden
            this.Write(" = context;\r\n");
            
            #line 84 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            
            #line 85 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\r\n");
            
            #line 87 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            
            #line 88 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (method.HasValidationParameter) { 
            
            #line default
            #line hidden
            this.Write(@"                if (validationErrors.Count > 0)
                {
                    return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse
                    {
                        StatusCode = 400
                    };
                }
");
            
            #line 96 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\r\n                ");
            
            #line 98 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(method.ResultType != null ? "var output = " : ""));
            
            #line default
            #line hidden
            
            #line 98 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(method.IsAsync ? "await " : ""));
            
            #line default
            #line hidden
            this.Write("function.");
            
            #line 98 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(method.Name));
            
            #line default
            #line hidden
            this.Write("(");
            
            #line 98 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
            this.Write(this.ToStringHelper.ToStringWithCulture(String.Join(", ", method.Parameters.Select((x, i) => $"p{i}"))));
            
            #line default
            #line hidden
            this.Write(");\r\n");
            
            #line 99 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (method.ResultType?.IsNullable ?? false) { 
            
            #line default
            #line hidden
            this.Write("                if (output == null)\r\n                {\r\n                    retur" +
                    "n new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 404 " +
                    "};\r\n                }\r\n");
            
            #line 104 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write("\r\n                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyRespon" +
                    "se\r\n                {\r\n");
            
            #line 108 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 if (method.ResultType != null) { 
            
            #line default
            #line hidden
            this.Write("                    Body = serializer.Serialize(output),\r\n                    Hea" +
                    "ders = new Dictionary<string, string> { { \"Content-Type\", \"application/json\" } }" +
                    ",\r\n");
            
            #line 111 "D:\GitHubTemplate\amazon-lambda-extension\Sandbox\WorkTemplate\Templates\LambdaTemplate.tt"
 } 
            
            #line default
            #line hidden
            this.Write(@"                    StatusCode = 200
                };
            }
            catch (WorkTemplate.ApiException ex)
            {
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = ex.StatusCode };
            }
            catch (System.Exception ex)
            {
                context.Logger.LogError(ex.ToString());
                return new Amazon.Lambda.APIGatewayEvents.APIGatewayProxyResponse { StatusCode = 500 };
            }
        }
    }
}
");
            return this.GenerationEnvironment.ToString();
        }
    }
    
    #line default
    #line hidden
    #region Base class
    /// <summary>
    /// Base class for this transformation
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TextTemplating", "17.0.0.0")]
    public class LambdaTemplateBase
    {
        #region Fields
        private global::System.Text.StringBuilder generationEnvironmentField;
        private global::System.CodeDom.Compiler.CompilerErrorCollection errorsField;
        private global::System.Collections.Generic.List<int> indentLengthsField;
        private string currentIndentField = "";
        private bool endsWithNewline;
        private global::System.Collections.Generic.IDictionary<string, object> sessionField;
        #endregion
        #region Properties
        /// <summary>
        /// The string builder that generation-time code is using to assemble generated output
        /// </summary>
        protected System.Text.StringBuilder GenerationEnvironment
        {
            get
            {
                if ((this.generationEnvironmentField == null))
                {
                    this.generationEnvironmentField = new global::System.Text.StringBuilder();
                }
                return this.generationEnvironmentField;
            }
            set
            {
                this.generationEnvironmentField = value;
            }
        }
        /// <summary>
        /// The error collection for the generation process
        /// </summary>
        public System.CodeDom.Compiler.CompilerErrorCollection Errors
        {
            get
            {
                if ((this.errorsField == null))
                {
                    this.errorsField = new global::System.CodeDom.Compiler.CompilerErrorCollection();
                }
                return this.errorsField;
            }
        }
        /// <summary>
        /// A list of the lengths of each indent that was added with PushIndent
        /// </summary>
        private System.Collections.Generic.List<int> indentLengths
        {
            get
            {
                if ((this.indentLengthsField == null))
                {
                    this.indentLengthsField = new global::System.Collections.Generic.List<int>();
                }
                return this.indentLengthsField;
            }
        }
        /// <summary>
        /// Gets the current indent we use when adding lines to the output
        /// </summary>
        public string CurrentIndent
        {
            get
            {
                return this.currentIndentField;
            }
        }
        /// <summary>
        /// Current transformation session
        /// </summary>
        public virtual global::System.Collections.Generic.IDictionary<string, object> Session
        {
            get
            {
                return this.sessionField;
            }
            set
            {
                this.sessionField = value;
            }
        }
        #endregion
        #region Transform-time helpers
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void Write(string textToAppend)
        {
            if (string.IsNullOrEmpty(textToAppend))
            {
                return;
            }
            // If we're starting off, or if the previous text ended with a newline,
            // we have to append the current indent first.
            if (((this.GenerationEnvironment.Length == 0) 
                        || this.endsWithNewline))
            {
                this.GenerationEnvironment.Append(this.currentIndentField);
                this.endsWithNewline = false;
            }
            // Check if the current text ends with a newline
            if (textToAppend.EndsWith(global::System.Environment.NewLine, global::System.StringComparison.CurrentCulture))
            {
                this.endsWithNewline = true;
            }
            // This is an optimization. If the current indent is "", then we don't have to do any
            // of the more complex stuff further down.
            if ((this.currentIndentField.Length == 0))
            {
                this.GenerationEnvironment.Append(textToAppend);
                return;
            }
            // Everywhere there is a newline in the text, add an indent after it
            textToAppend = textToAppend.Replace(global::System.Environment.NewLine, (global::System.Environment.NewLine + this.currentIndentField));
            // If the text ends with a newline, then we should strip off the indent added at the very end
            // because the appropriate indent will be added when the next time Write() is called
            if (this.endsWithNewline)
            {
                this.GenerationEnvironment.Append(textToAppend, 0, (textToAppend.Length - this.currentIndentField.Length));
            }
            else
            {
                this.GenerationEnvironment.Append(textToAppend);
            }
        }
        /// <summary>
        /// Write text directly into the generated output
        /// </summary>
        public void WriteLine(string textToAppend)
        {
            this.Write(textToAppend);
            this.GenerationEnvironment.AppendLine();
            this.endsWithNewline = true;
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void Write(string format, params object[] args)
        {
            this.Write(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Write formatted text directly into the generated output
        /// </summary>
        public void WriteLine(string format, params object[] args)
        {
            this.WriteLine(string.Format(global::System.Globalization.CultureInfo.CurrentCulture, format, args));
        }
        /// <summary>
        /// Raise an error
        /// </summary>
        public void Error(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Raise a warning
        /// </summary>
        public void Warning(string message)
        {
            System.CodeDom.Compiler.CompilerError error = new global::System.CodeDom.Compiler.CompilerError();
            error.ErrorText = message;
            error.IsWarning = true;
            this.Errors.Add(error);
        }
        /// <summary>
        /// Increase the indent
        /// </summary>
        public void PushIndent(string indent)
        {
            if ((indent == null))
            {
                throw new global::System.ArgumentNullException("indent");
            }
            this.currentIndentField = (this.currentIndentField + indent);
            this.indentLengths.Add(indent.Length);
        }
        /// <summary>
        /// Remove the last indent that was added with PushIndent
        /// </summary>
        public string PopIndent()
        {
            string returnValue = "";
            if ((this.indentLengths.Count > 0))
            {
                int indentLength = this.indentLengths[(this.indentLengths.Count - 1)];
                this.indentLengths.RemoveAt((this.indentLengths.Count - 1));
                if ((indentLength > 0))
                {
                    returnValue = this.currentIndentField.Substring((this.currentIndentField.Length - indentLength));
                    this.currentIndentField = this.currentIndentField.Remove((this.currentIndentField.Length - indentLength));
                }
            }
            return returnValue;
        }
        /// <summary>
        /// Remove any indentation
        /// </summary>
        public void ClearIndent()
        {
            this.indentLengths.Clear();
            this.currentIndentField = "";
        }
        #endregion
        #region ToString Helpers
        /// <summary>
        /// Utility class to produce culture-oriented representation of an object as a string.
        /// </summary>
        public class ToStringInstanceHelper
        {
            private System.IFormatProvider formatProviderField  = global::System.Globalization.CultureInfo.InvariantCulture;
            /// <summary>
            /// Gets or sets format provider to be used by ToStringWithCulture method.
            /// </summary>
            public System.IFormatProvider FormatProvider
            {
                get
                {
                    return this.formatProviderField ;
                }
                set
                {
                    if ((value != null))
                    {
                        this.formatProviderField  = value;
                    }
                }
            }
            /// <summary>
            /// This is called from the compile/run appdomain to convert objects within an expression block to a string
            /// </summary>
            public string ToStringWithCulture(object objectToConvert)
            {
                if ((objectToConvert == null))
                {
                    throw new global::System.ArgumentNullException("objectToConvert");
                }
                System.Type t = objectToConvert.GetType();
                System.Reflection.MethodInfo method = t.GetMethod("ToString", new System.Type[] {
                            typeof(System.IFormatProvider)});
                if ((method == null))
                {
                    return objectToConvert.ToString();
                }
                else
                {
                    return ((string)(method.Invoke(objectToConvert, new object[] {
                                this.formatProviderField })));
                }
            }
        }
        private ToStringInstanceHelper toStringHelperField = new ToStringInstanceHelper();
        /// <summary>
        /// Helper to produce culture-oriented representation of an object as a string
        /// </summary>
        public ToStringInstanceHelper ToStringHelper
        {
            get
            {
                return this.toStringHelperField;
            }
        }
        #endregion
    }
    #endregion
}
