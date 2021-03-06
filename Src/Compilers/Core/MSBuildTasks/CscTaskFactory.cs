﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// CscTaskFactory for the preview is necessary because MsBuild ships in Dev12, and we cannot change it's target files.
    /// MsBuild calls this TaskFactory to create the Roslyn Compiler Task
    /// </summary>
    public sealed class CscTaskFactory : RoslynTaskFactory<Csc>
    {
        protected override Csc CreateTask(string vsSessionGuid)
        {
            return new Csc { VsSessionGuid = vsSessionGuid };
        }
    }
}
