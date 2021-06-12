﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ExcisePlaning.ServiceReference1 {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://dexsrvint.excise.go.th/wsdl/LDAPGateway/LDPAGAuthenAndGetUserRole", ConfigurationName="ServiceReference1.LDPAGAuthenAndGetUserRolePortType")]
    public interface LDPAGAuthenAndGetUserRolePortType {
        
        // CODEGEN: Generating message contract since the operation LDPAGAuthenAndGetUserRoleOperation is neither RPC nor document wrapped.
        [System.ServiceModel.OperationContractAttribute(Action="", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        [System.ServiceModel.ServiceKnownTypeAttribute(typeof(UserInfoBase))]
        ExcisePlaning.ServiceReference1.output1 LDPAGAuthenAndGetUserRoleOperation(ExcisePlaning.ServiceReference1.input1 request);
        
        [System.ServiceModel.OperationContractAttribute(Action="", ReplyAction="*")]
        System.Threading.Tasks.Task<ExcisePlaning.ServiceReference1.output1> LDPAGAuthenAndGetUserRoleOperationAsync(ExcisePlaning.ServiceReference1.input1 request);
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://dexsrvint.excise.go.th/schema/AuthenAndGetUserRole")]
    public partial class AuthenAndGetUserRoleRequest : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string userIdField;
        
        private string passwordField;
        
        private string applicationIdField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string userId {
            get {
                return this.userIdField;
            }
            set {
                this.userIdField = value;
                this.RaisePropertyChanged("userId");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string password {
            get {
                return this.passwordField;
            }
            set {
                this.passwordField = value;
                this.RaisePropertyChanged("password");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string applicationId {
            get {
                return this.applicationIdField;
            }
            set {
                this.applicationIdField = value;
                this.RaisePropertyChanged("applicationId");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://dexsrvint.excise.go.th/schema/LdapUserBase")]
    public partial class RoleBase : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string roleNameField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string roleName {
            get {
                return this.roleNameField;
            }
            set {
                this.roleNameField = value;
                this.RaisePropertyChanged("roleName");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://dexsrvint.excise.go.th/schema/LdapUserBase")]
    public partial class MessageBase : object, System.ComponentModel.INotifyPropertyChanged {
        
        private bool successField;
        
        private string codeField;
        
        private string descriptionField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public bool success {
            get {
                return this.successField;
            }
            set {
                this.successField = value;
                this.RaisePropertyChanged("success");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string code {
            get {
                return this.codeField;
            }
            set {
                this.codeField = value;
                this.RaisePropertyChanged("code");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string description {
            get {
                return this.descriptionField;
            }
            set {
                this.descriptionField = value;
                this.RaisePropertyChanged("description");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlIncludeAttribute(typeof(AuthenAndGetUserRoleResponse))]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://dexsrvint.excise.go.th/schema/LdapUserBase")]
    public partial class UserInfoBase : object, System.ComponentModel.INotifyPropertyChanged {
        
        private string userThaiIdField;
        
        private string userThaiNameField;
        
        private string userThaiSurnameField;
        
        private string userEngNameField;
        
        private string userEngSurnameField;
        
        private string titleField;
        
        private string userIdField;
        
        private string emailField;
        
        private string cnNameField;
        
        private string telephoneNoField;
        
        private string officeIdField;
        
        private string accessAttrField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public string userThaiId {
            get {
                return this.userThaiIdField;
            }
            set {
                this.userThaiIdField = value;
                this.RaisePropertyChanged("userThaiId");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public string userThaiName {
            get {
                return this.userThaiNameField;
            }
            set {
                this.userThaiNameField = value;
                this.RaisePropertyChanged("userThaiName");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public string userThaiSurname {
            get {
                return this.userThaiSurnameField;
            }
            set {
                this.userThaiSurnameField = value;
                this.RaisePropertyChanged("userThaiSurname");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=3)]
        public string userEngName {
            get {
                return this.userEngNameField;
            }
            set {
                this.userEngNameField = value;
                this.RaisePropertyChanged("userEngName");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=4)]
        public string userEngSurname {
            get {
                return this.userEngSurnameField;
            }
            set {
                this.userEngSurnameField = value;
                this.RaisePropertyChanged("userEngSurname");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=5)]
        public string title {
            get {
                return this.titleField;
            }
            set {
                this.titleField = value;
                this.RaisePropertyChanged("title");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=6)]
        public string userId {
            get {
                return this.userIdField;
            }
            set {
                this.userIdField = value;
                this.RaisePropertyChanged("userId");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=7)]
        public string email {
            get {
                return this.emailField;
            }
            set {
                this.emailField = value;
                this.RaisePropertyChanged("email");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=8)]
        public string cnName {
            get {
                return this.cnNameField;
            }
            set {
                this.cnNameField = value;
                this.RaisePropertyChanged("cnName");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=9)]
        public string telephoneNo {
            get {
                return this.telephoneNoField;
            }
            set {
                this.telephoneNoField = value;
                this.RaisePropertyChanged("telephoneNo");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=10)]
        public string officeId {
            get {
                return this.officeIdField;
            }
            set {
                this.officeIdField = value;
                this.RaisePropertyChanged("officeId");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=11)]
        public string accessAttr {
            get {
                return this.accessAttrField;
            }
            set {
                this.accessAttrField = value;
                this.RaisePropertyChanged("accessAttr");
            }
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        
        protected void RaisePropertyChanged(string propertyName) {
            System.ComponentModel.PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
            if ((propertyChanged != null)) {
                propertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.3752.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://dexsrvint.excise.go.th/schema/AuthenAndGetUserRole")]
    public partial class AuthenAndGetUserRoleResponse : UserInfoBase {
        
        private MessageBase messageField;
        
        private RoleBase[] rolesField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public MessageBase message {
            get {
                return this.messageField;
            }
            set {
                this.messageField = value;
                this.RaisePropertyChanged("message");
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayAttribute(Order=1)]
        [System.Xml.Serialization.XmlArrayItemAttribute("role", Namespace="http://dexsrvint.excise.go.th/schema/LdapUserBase", IsNullable=false)]
        public RoleBase[] roles {
            get {
                return this.rolesField;
            }
            set {
                this.rolesField = value;
                this.RaisePropertyChanged("roles");
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class input1 {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://dexsrvint.excise.go.th/schema/AuthenAndGetUserRole", Order=0)]
        public ExcisePlaning.ServiceReference1.AuthenAndGetUserRoleRequest RequestObj;
        
        public input1() {
        }
        
        public input1(ExcisePlaning.ServiceReference1.AuthenAndGetUserRoleRequest RequestObj) {
            this.RequestObj = RequestObj;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(IsWrapped=false)]
    public partial class output1 {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://dexsrvint.excise.go.th/schema/AuthenAndGetUserRole", Order=0)]
        public ExcisePlaning.ServiceReference1.AuthenAndGetUserRoleResponse ResponseObj;
        
        public output1() {
        }
        
        public output1(ExcisePlaning.ServiceReference1.AuthenAndGetUserRoleResponse ResponseObj) {
            this.ResponseObj = ResponseObj;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface LDPAGAuthenAndGetUserRolePortTypeChannel : ExcisePlaning.ServiceReference1.LDPAGAuthenAndGetUserRolePortType, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class LDPAGAuthenAndGetUserRolePortTypeClient : System.ServiceModel.ClientBase<ExcisePlaning.ServiceReference1.LDPAGAuthenAndGetUserRolePortType>, ExcisePlaning.ServiceReference1.LDPAGAuthenAndGetUserRolePortType {
        
        public LDPAGAuthenAndGetUserRolePortTypeClient() {
        }
        
        public LDPAGAuthenAndGetUserRolePortTypeClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public LDPAGAuthenAndGetUserRolePortTypeClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public LDPAGAuthenAndGetUserRolePortTypeClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public LDPAGAuthenAndGetUserRolePortTypeClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        ExcisePlaning.ServiceReference1.output1 ExcisePlaning.ServiceReference1.LDPAGAuthenAndGetUserRolePortType.LDPAGAuthenAndGetUserRoleOperation(ExcisePlaning.ServiceReference1.input1 request) {
            return base.Channel.LDPAGAuthenAndGetUserRoleOperation(request);
        }
        
        public ExcisePlaning.ServiceReference1.AuthenAndGetUserRoleResponse LDPAGAuthenAndGetUserRoleOperation(ExcisePlaning.ServiceReference1.AuthenAndGetUserRoleRequest RequestObj) {
            ExcisePlaning.ServiceReference1.input1 inValue = new ExcisePlaning.ServiceReference1.input1();
            inValue.RequestObj = RequestObj;
            ExcisePlaning.ServiceReference1.output1 retVal = ((ExcisePlaning.ServiceReference1.LDPAGAuthenAndGetUserRolePortType)(this)).LDPAGAuthenAndGetUserRoleOperation(inValue);
            return retVal.ResponseObj;
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<ExcisePlaning.ServiceReference1.output1> ExcisePlaning.ServiceReference1.LDPAGAuthenAndGetUserRolePortType.LDPAGAuthenAndGetUserRoleOperationAsync(ExcisePlaning.ServiceReference1.input1 request) {
            return base.Channel.LDPAGAuthenAndGetUserRoleOperationAsync(request);
        }
        
        public System.Threading.Tasks.Task<ExcisePlaning.ServiceReference1.output1> LDPAGAuthenAndGetUserRoleOperationAsync(ExcisePlaning.ServiceReference1.AuthenAndGetUserRoleRequest RequestObj) {
            ExcisePlaning.ServiceReference1.input1 inValue = new ExcisePlaning.ServiceReference1.input1();
            inValue.RequestObj = RequestObj;
            return ((ExcisePlaning.ServiceReference1.LDPAGAuthenAndGetUserRolePortType)(this)).LDPAGAuthenAndGetUserRoleOperationAsync(inValue);
        }
    }
}
