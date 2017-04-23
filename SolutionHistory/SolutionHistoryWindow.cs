//------------------------------------------------------------------------------
// <copyright file="SolutionHistoryWindow.cs" company="Microsoft">
//     Copyright (c) Microsoft.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace SolutionHistory
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("d4c1ae38-8a0c-466a-8b39-a0ec4409ebb0")]
    public class SolutionHistoryWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionHistoryWindow"/> class.
        /// </summary>
        public SolutionHistoryWindow() : base(null)
        {
            this.Caption = "Solution History";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new SolutionHistoryWindowControl();
        }
    }
}
