using System.Collections.Generic;
using System.Linq;
using  Typewriter.Configuration;

//using EnvDTE;

namespace Typewriter.CodeModel.Configuration
{
    public class SettingsImpl : Settings
    {
        //private readonly ProjectItem _projectItem;

//        public SettingsImpl(ProjectItem projectItem)
//        {
//            _projectItem = projectItem;
//        }

        private List<string> _includedProjects;
        
        public override Settings IncludeProject(string projectName)
        {
            if (_includedProjects == null)
                _includedProjects = new List<string>();

            //ProjectHelpers.AddProject(_projectItem, _includedProjects, projectName);
            return this;
        }
        
        public Settings IncludePath(string referencePath)
        {
            if (_includedProjects == null)
                _includedProjects = new List<string>();

            //ProjectHelpers.AddProject(_projectItem, _includedProjects, projectName);
            var files = System.IO.Directory.GetFiles(referencePath);
            foreach (var file in files.Where(i => i.EndsWith(".dll")))
            {
                _includedProjects.Add(file);   
            }
            return this;
        }
        
        public override Settings IncludeReferencedProjects()
        {
            if (_includedProjects == null)
                _includedProjects = new List<string>();

            //ProjectHelpers.AddReferencedProjects(_includedProjects, _projectItem);
            return this;
        }

        public override Settings IncludeCurrentProject()
        {
            if (_includedProjects == null)
                _includedProjects = new List<string>();

            //ProjectHelpers.AddCurrentProject(_includedProjects, _projectItem);
            return this;
        }

        public override Settings IncludeAllProjects()
        {
            if (_includedProjects == null)
                _includedProjects = new List<string>();

            //ProjectHelpers.AddAllProjects(_projectItem.DTE, _includedProjects);
            return this;
        }

        public override ICollection<string> IncludedProjects
        {
            get
            {
                if (_includedProjects == null)
                {
                    IncludeCurrentProject();
                    IncludeReferencedProjects();
                }

                return _includedProjects;
            }
        }
    }
}
