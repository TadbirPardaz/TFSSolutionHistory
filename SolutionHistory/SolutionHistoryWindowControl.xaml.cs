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
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

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

            object slnAddress;
            sln.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out slnAddress);

            WorkspaceInfo wi = Workstation.Current.GetLocalWorkspaceInfo(slnAddress.ToString());

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

        public class HistoryItem
        {
            public string User { get; set; }
            public int Changeset { get; set; }
            public string Comment { get; set; }
            public DateTime Date { get; internal set; }
            public string Project { get; set; }
        }

        private async void Changeset_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var historyItem = (sender as DataGridRow).DataContext as HistoryItem;

            var changeset = await GetChangesetWebUrl(historyItem.Changeset);

            var link = changeset.Links.Links["web"] as Microsoft.VisualStudio.Services.WebApi.ReferenceLink;

            System.Diagnostics.Process.Start(link.Href);
        }
    }
}