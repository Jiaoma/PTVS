﻿// Python Tools for Visual Studio
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
// IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Debugger.DebugAdapterHost.Interfaces;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace Microsoft.PythonTools.Debugger {
    [ComVisible(true)]
    [Guid(CustomProtocolExtensionCLSIDNoBraces)]
    public class CustomDebugAdapterProtocolExtension : ICustomProtocolExtension { 
        private IDebugAdapterHostContext _context;
        private IProtocolHostOperations _hostOperations;

        public const string CustomProtocolExtensionCLSIDNoBraces = "A5E59A97-43B8-4B65-833A-5300076553E1";
        public const string CustomProtocolExtensionCLSID = "{" + CustomProtocolExtensionCLSIDNoBraces + "}";
        public CustomDebugAdapterProtocolExtension() {}

        private static CustomDebugAdapterProtocolExtension Evaluator { get; set; }
        public static bool CanUseExperimental() {
            return Evaluator != null;
        }

        public static string GetCurrentFrameFilename(int threadId) {
            var stackTraceResponse = Evaluator?._hostOperations.SendRequestSync(new StackTraceRequest(threadId));
            return stackTraceResponse.StackFrames[0].Source.Path;
        }

        public static string EvaluateReplRequest(string expression, int threadId) {
            var stackTraceResponse = Evaluator?._hostOperations.SendRequestSync(new StackTraceRequest(threadId));
            var fid = stackTraceResponse.StackFrames[0].Id;
            var response = Evaluator?._hostOperations.SendRequestSync(new EvaluateRequest(expression.Replace("\n", "@LINE@"), frameId: fid, context: EvaluateArguments.ContextValue.Repl));
            return response?.Result;
        }

        public static void SendRequest<TArgs, TResponse>(DebugRequestWithResponse<TArgs, TResponse> request, Action<TArgs, TResponse> completionFunc, Action<TArgs, ProtocolException> errorFunc = null)
            where TArgs : class, new()
            where TResponse : ResponseBody {
            Evaluator?._hostOperations.SendRequest(request, completionFunc, errorFunc);
        }
       
        public static void SendRequest<TArgs>(DebugRequest<TArgs> request, Action<TArgs> completionFunc, Action<TArgs, ProtocolException> errorFunc = null) 
            where TArgs : class, new() {
            Evaluator?._hostOperations.SendRequest(request, completionFunc, errorFunc);
        }
        
        public static TResponse SendRequestSync<TArgs, TResponse>(DebugRequestWithResponse<TArgs, TResponse> request)
            where TArgs : class, new()
            where TResponse : ResponseBody {
            return Evaluator?._hostOperations.SendRequestSync(request);
        }

        public static void SendRequestSync<TArgs>(DebugRequest<TArgs> request) 
            where TArgs : class, new() {
            Evaluator?._hostOperations.SendRequestSync(request);
        }



        public void Initialize(IDebugAdapterHostContext context) {
            _context = context ?? throw new ArgumentException(nameof(context));
            _context.Events.DebuggingEnded += OnDebuggingEnded;
            Evaluator = this;
        }

        private void OnDebuggingEnded(object sender, EventArgs e) {
            Evaluator = null;
        }

        public void RegisterCustomMessages(ICustomMessageRegistry registry, IProtocolHostOperations hostOperations) {
            _hostOperations = hostOperations;
        }
    }
}
