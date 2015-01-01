using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RP.Util.EnvDte
{
    using System.Diagnostics.CodeAnalysis;

    using EnvDTE;

    using Microsoft.VisualStudio.TextTemplating;

    public static class vsProjectType
    {
        public const string SolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
        public const string VisualBasic = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
        public const string VisualCSharp = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        public const string VisualCPlusPlus = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
        public const string VisualJSharp = "{E6FDF86B-F3D1-11D4-8576-0002A516ECE8}";
        public const string WebProject = "{E24C65DC-7377-472b-9ABA-BC803B73C61A}";
    }

    public class EnvDteHelper: IDisposable
    {
        private IServiceProvider host;

        private DTE dte;

        private Solution solution;

        public EnvDteHelper(IServiceProvider host)
        {
            if(host == null)
                throw new ArgumentNullException("host");

            dte = (DTE)host.GetService(typeof(DTE));

            if(dte == null)
                throw new Exception("Could not create a DTE object, this helper must be run through the visual studio host");
        }

        private DTE Dte
        {
            get
            {
                if(dte == null)
                    throw new ObjectDisposedException("The EnvDteHelper has been disposed");

                return dte;
            }
        }

        public Solution Solution
        {
            get
            {
                if(dte == null)
                    throw new ObjectDisposedException("The EnvDteHelper has been disposed");

                if (solution == null)
                {
                    solution = Dte.Solution;
                    if (!solution.IsOpen)
                    {
                        throw new InvalidOperationException("We expected the solution to be open");
                    }
                }

                return solution;
            }
        }

        public IEnumerable<Project> Projects
        {
            get
            {
                Projects projects = Solution.Projects;

                var list = new List<Project>();

                var item = projects.GetEnumerator();

                while (item.MoveNext())
                {
                    var project = item.Current as Project;
                    if (project == null)
                    {
                        continue;
                    }

                    if (project.Kind == vsProjectType.SolutionFolder)
                    {
                        list.AddRange(GetSolutionFolderProjects(project));
                    }
                    else
                    {
                        list.Add(project);
                    }
                }

                return list;
            }
        }

        public IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            List<Project> list = new List<Project>();

            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (subProject.Kind == vsProjectType.SolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    list.Add(subProject);
                }
            }

            return list;
        }

        private static IEnumerable<T> Descendants<T>(IEnumerable<T> source, Func<T, IEnumerable<T>> descendBy)
        {
            foreach (T value in source)
            {
                yield return value;
                foreach (T child in Descendants(descendBy(value), descendBy))
                {
                    yield return child;
                }
            }
        }

        public IEnumerable<ProjectItem> GetProjectItems(Project proj)
        {
            return Descendants(proj.ProjectItems.Cast<ProjectItem>(), pi => pi.ProjectItems.Cast<ProjectItem>());
        }

        public IEnumerable<ProjectItem> GetProjectItems()
        {
            return Projects.SelectMany(project => GetProjectItems(project));
        }

        public string SolutionFile
        {
            get
            {
                return Solution.FileName;
            }
        }

        public string SolutionFileName
        {
            get
            {
                return System.IO.Path.GetFileName(Solution.FileName);
            }
        }

        public string SolutionName
        {
            get
            {
                return Solution.Properties.Item("Name").Value.ToString();
            }
        }

        public Project GetProject(string projectName)
        {
            return Projects.First(p => p.Name == projectName);
        }

        ///// <summary>
        ///// Gets the project containing the .tt-File
        ///// </summary>
        //public Project CurrentProject(object host)
        //{
        //    return GetProject(((ITextTemplatingEngineHost)host).TemplateFile).ContainingProject;
        //}

    //#region Project Items
    //public EnvDTE.ProjectItem FindProjectItem(string fileName)
    //{
    //    return this.DTE.Solution.FindProjectItem(fileName);
    //}
    ///// <summary>
    ///// Gets all project items from the current solution
    ///// </summary>
    //public IEnumerable<EnvDTE.ProjectItem>GetAllSolutionItems()
    //{
    //    var ret = new List<EnvDTE.ProjectItem>();

    //    // iterate all projects and add their items
    //    foreach(EnvDTE.Project project in this.GetAllProjects())
    //        ret.AddRange(GetAllProjectItems(project));

    //    return ret;
    //}
    ///// <summary>
    ///// Gets all project items from the current project
    ///// </summary>
    //public IEnumerable<EnvDTE.ProjectItem>GetAllProjectItems()
    //{
    //    // get the project of the template file and reeturn all its items
    //    var project = this.CurrentProject;
    //    return GetAllProjectItems(project);
    //}
    ///// <summary>
    ///// Gets all Project items from a given project. 
    ///// </summary>
    //public IEnumerable<EnvDTE.ProjectItem>GetAllProjectItems(EnvDTE.Project project)
    //{
    //    return this.GetProjectItemsRecursively(project.ProjectItems);
    //}
    //#endregion

    //#region Code Model
    ///// <summary>
    ///// Searches a given collection of CodeElements recursively for objects of the given elementType.
    ///// </summary>
    ///// <param name="elements">Collection of CodeElements to recursively search for matching objects in.</param>
    ///// <param name="elementType">Objects of this CodeModelElement-Type will be returned.</param>
    ///// <param name="includeExternalTypes">If set to true objects that are not part of this solution are retrieved, too. E.g. the INotifyPropertyChanged interface from the System.ComponentModel namespace.</param>
    ///// <returns>A list of CodeElement objects matching the desired elementType.</returns>
    //public List<EnvDTE.CodeElement> GetAllCodeElementsOfType(EnvDTE.CodeElements elements, EnvDTE.vsCMElement elementType, bool includeExternalTypes)
    //{
    //    var ret = new List<EnvDTE.CodeElement>();

    //    foreach (EnvDTE.CodeElement elem in elements)
    //    {
    //        // iterate all namespaces (even if they are external)
    //        // > they might contain project code
    //        if (elem.Kind == EnvDTE.vsCMElement.vsCMElementNamespace)
    //        {
    //            ret.AddRange(GetAllCodeElementsOfType(((EnvDTE.CodeNamespace)elem).Members, elementType, includeExternalTypes));
    //        }
    //        // if its not a namespace but external
    //        // > ignore it
    //        else if (elem.InfoLocation == EnvDTE.vsCMInfoLocation.vsCMInfoLocationExternal
    //                && !includeExternalTypes)
    //            continue;
    //        // if its from the project
    //        // > check its members
    //        else if (elem.IsCodeType)
    //        {
    //            ret.AddRange(GetAllCodeElementsOfType(((EnvDTE.CodeType)elem).Members, elementType, includeExternalTypes));
    //        }

    //        // if this item is of the desired type
    //        // > store it
    //        if (elem.Kind == elementType)
    //            ret.Add(elem);
    //    }

    //    return ret;
    //}
    //#endregion


    //#region Auxiliary stuff
    //private List<EnvDTE.Project> GetProjectsFromItemsCollection(EnvDTE.ProjectItems items)
    //{
    //    var ret = new List<EnvDTE.Project>();

    //    foreach(EnvDTE.ProjectItem item in items)
    //    {
    //        if (item.SubProject == null)
    //            continue;
    //        else if (item.SubProject.Kind == vsProjectType.SolutionFolder)
    //            ret.AddRange(GetProjectsFromItemsCollection(item.SubProject.ProjectItems));
    //        else if (item.SubProject.Kind == vsProjectType.VisualBasic
    //              || item.SubProject.Kind == vsProjectType.VisualCPlusPlus
    //              || item.SubProject.Kind == vsProjectType.VisualCSharp
    //              || item.SubProject.Kind == vsProjectType.VisualJSharp
    //              || item.SubProject.Kind == vsProjectType.WebProject)
    //            ret.Add(item.SubProject);
    //    }

    //    return ret;
    //}
    //private List<EnvDTE.ProjectItem> GetProjectItemsRecursively(EnvDTE.ProjectItems items)
    //{
    //    var ret = new List<EnvDTE.ProjectItem>();
    //    if (items == null) return ret;
		
    //    foreach(EnvDTE.ProjectItem item in items)
    //    {
    //        ret.Add(item);
    //        ret.AddRange(GetProjectItemsRecursively(item.ProjectItems));
    //    }

    //    return ret;
    //}
        public void Dispose()
        {
            solution = null;
            dte = null;
            host = null;
        }
    }
}
