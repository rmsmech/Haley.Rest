﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Haley.Enums;
using Haley.Models;

namespace Haley.Abstractions {
    public interface IProgressReporter {
        Guid Id { get; }
        event EventHandler<ProgressTracker> TrackerInitialized;
        event EventHandler<(string id, ProgressState state)> StateChanged;
        event EventHandler<(string id, long consumed_size)> ProgressChanged;
        void ChangeProgress(string id, long consumed_size);
        void InitializeTracker(ProgressTracker tracker);
        void ChangeState(string id, ProgressState state);
    }
}
