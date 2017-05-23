//------------------------------------------------------------------------------
// <copyright file="SolutionHistoryWindowControl.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using EnvDTE;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SolutionHistory.Models;

namespace SolutionHistory
{
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for SolutionHistoryWindowControl.
    /// </summary>
    public partial class SolutionHistoryWindowControl : UserControl
    {
        ViewChangesetMode viewChangeset = ViewChangesetMode.Web;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionHistoryWindowControl"/> class.
        /// </summary>
        public SolutionHistoryWindowControl()
        {
            this.InitializeComponent();
        }



        private async void ViewHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var data = await GetSolutionHistory();
                dgHistory.ItemsSource = data;
            }
            catch (Exception ex)
            {
                dgHistory.ItemsSource = null;

                VsShellUtilities.ShowMessageBox(
                    ServiceProvider.GlobalProvider,
                    ex.Message,
                    "Error",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        public async Task<IEnumerable<HistoryItem>> GetSolutionHistory()
        {

            var sln = ServiceProvider.GlobalProvider.GetService(typeof(IVsSolution)) as IVsSolution;

            uint projectCount = 0;
            int hr = sln.GetProjectFilesInSolution((uint)__VSGETPROJFILESFLAGS.GPFF_SKIPUNLOADEDPROJECTS, 0, null,
                out projectCount);

            string[] projectNames = new string[projectCount];
            hr = sln.GetProjectFilesInSolution((uint)__VSGETPROJFILESFLAGS.GPFF_SKIPUNLOADEDPROJECTS, projectCount,
                projectNames, out projectCount);

            projectNames = projectNames.Where(System.IO.File.Exists).ToArray();

            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;


            WorkspaceInfo wi = GetCurrentWorkspace();

            var collectionUri = wi.ServerUri;

            VssConnection connection = new VssConnection(collectionUri, new VssCredentials());

            var ss = connection.GetClient<TfvcHttpClient>();

            var list = new ConcurrentBag<HistoryItem>();

            int current = 0;
            int count = projectNames.Length;

            foreach (var project in projectNames)
            {
                var searchParam = new TfvcChangesetSearchCriteria()
                {
                    ItemPath = ConvertToItemPath(wi, Path.GetDirectoryName(project)),
                    Author = string.IsNullOrWhiteSpace(author.Text) ? null : author.Text
                };
                var history = await ss.GetChangesetsAsync(searchCriteria: searchParam, maxChangeCount: 10);

                ProgressBar.Value = ((current + 1) / (float)count) * 100;

                if (ProgressBar.Value != 100)
                    ProgressBar.Visibility = Visibility.Visible;
                else
                {
                    ProgressBar.Visibility = Visibility.Collapsed;
                }

                history.Select(x => new HistoryItem()
                {
                    Changeset = x.ChangesetId,
                    User = x.Author.DisplayName,
                    Comment = x.Comment,
                    Date = x.CreatedDate,
                    Project = Path.GetFileNameWithoutExtension(project),
                }).ToList()
                .ForEach(list.Add);

                current++;
            }

            return list.GroupBy(x => x.Changeset).Select(x => x.First()).OrderByDescending(x => x.Changeset);
        }

        private WorkspaceInfo GetCurrentWorkspace()
        {
            var sln = ServiceProvider.GlobalProvider.GetService(typeof(IVsSolution)) as IVsSolution;

            object slnAddress;
            sln.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out slnAddress);

            return Workstation.Current.GetLocalWorkspaceInfo(slnAddress.ToString());
        }

        public async Task<TfvcChangeset> GetChangesetWebUrl(int id)
        {
            var sln = ServiceProvider.GlobalProvider.GetService(typeof(IVsSolution)) as IVsSolution;

            uint projectCount = 0;
            int hr = sln.GetProjectFilesInSolution((uint)__VSGETPROJFILESFLAGS.GPFF_SKIPUNLOADEDPROJECTS, 0, null,
                out projectCount);

            string[] projectNames = new string[projectCount];
            hr = sln.GetProjectFilesInSolution((uint)__VSGETPROJFILESFLAGS.GPFF_SKIPUNLOADEDPROJECTS, projectCount,
                projectNames, out projectCount);

            projectNames = projectNames.Where(System.IO.File.Exists).ToArray();

            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;

            object slnAddress;
            sln.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out slnAddress);

            WorkspaceInfo wi = Workstation.Current.GetLocalWorkspaceInfo(slnAddress.ToString());

            var collectionUri = wi.ServerUri;

            VssConnection connection = new VssConnection(collectionUri, new VssCredentials());

            var ss = connection.GetClient<TfvcHttpClient>();

            return await ss.GetChangesetAsync(id);
        }

        public string ConvertToItemPath(WorkspaceInfo wsp, string path)
        {
            var workspace = wsp.GetWorkspace(new TfsTeamProjectCollection(wsp.ServerUri));
            //string root = wsp.MappedPaths.First(x => path.StartsWith(x));
            //return "$/" + path.Replace(root, "").Replace("\\", "/");
            return workspace.GetServerItemForLocalItem(path);
        }

        private async void Changeset_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var historyItem = (sender as DataGridRow).DataContext as HistoryItem;

            Func<int, System.Threading.Tasks.Task> viewChangesetFunc = ViewChangesetDetails_Web;

            switch (viewChangeset)
            {
                case ViewChangesetMode.TeamExplorer:
                    viewChangesetFunc = ViewChangesetDetails_TeamExplorer;
                    break;
                case ViewChangesetMode.Popup:
                    viewChangesetFunc = ViewChangesetDetails_Popup;
                    break;
                case ViewChangesetMode.Web:
                    viewChangesetFunc = ViewChangesetDetails_Web;
                    break;
            }

            await viewChangesetFunc(historyItem.Changeset);
        }

        #region View Changeset Implementations
        private async System.Threading.Tasks.Task ViewChangesetDetails_Web(int changesetId)
        {
            var changeset = await GetChangesetWebUrl(changesetId);

            var link = changeset.Links.Links["web"] as Microsoft.VisualStudio.Services.WebApi.ReferenceLink;

            System.Diagnostics.Process.Start(link.Href);
        }

        private async System.Threading.Tasks.Task ViewChangesetDetails_Popup(int changesetId)
        {
            DTE2 dte = (DTE2)Package.GetGlobalService(typeof(DTE));

            var vce = dte.DTE.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt");

            var methodInfo = vce.GetType().GetMethod("ViewChangesetDetails", BindingFlags.Instance | BindingFlags.Public);
            methodInfo.Invoke(vce, new object[] { changesetId });

            await System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task ViewChangesetDetails_TeamExplorer(int changesetId)
        {
            ITeamExplorer teamExplorer = ServiceProvider.GlobalProvider.GetService(typeof(ITeamExplorer)) as ITeamExplorer;
            if (teamExplorer != null)
            {
                teamExplorer.NavigateToPage(new Guid(TeamExplorerPageIds.ChangesetDetails), changesetId);
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }
        #endregion

    }
}