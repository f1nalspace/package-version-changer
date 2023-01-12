using System;
using DevExpress.Mvvm;

namespace TSP.PackageVersionChanger
{
    public class EditProjectViewModel : ViewModelBase
    {
        public CSharpProject Project { get; }

        public bool IsChecked { get => GetValue<bool>(); set => SetValue(value); }

        public EditProjectViewModel(CSharpProject project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));
            Project = project;
            IsChecked = true;
        }

        public override string ToString() => Project.Name;
    }
}
