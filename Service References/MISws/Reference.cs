﻿//------------------------------------------------------------------------------
// <auto-generated>
//     這段程式碼是由工具產生的。
//     執行階段版本:4.0.30319.42000
//
//     對這個檔案所做的變更可能會造成錯誤的行為，而且如果重新產生程式碼，
//     變更將會遺失。
// </auto-generated>
//------------------------------------------------------------------------------

namespace ZOOM_REST_Web.MISws {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="MISws.IBillingAPI")]
    public interface IBillingAPI {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IBillingAPI/Execute", ReplyAction="http://tempuri.org/IBillingAPI/ExecuteResponse")]
        string Execute(string value);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IBillingAPIChannel : ZOOM_REST_Web.MISws.IBillingAPI, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class BillingAPIClient : System.ServiceModel.ClientBase<ZOOM_REST_Web.MISws.IBillingAPI>, ZOOM_REST_Web.MISws.IBillingAPI {
        
        public BillingAPIClient() {
        }
        
        public BillingAPIClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public BillingAPIClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public BillingAPIClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public BillingAPIClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public string Execute(string value) {
            return base.Channel.Execute(value);
        }
    }
}