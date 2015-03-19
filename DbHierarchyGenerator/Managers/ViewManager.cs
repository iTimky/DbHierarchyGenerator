using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DbHierarchyGenerator.Models;

namespace DbHierarchyGenerator.Managers
{
    public class ViewManager
    {
        public void FillDependecies(List<View> views)
        {
            foreach (var view in views)
            {
                view.ParentViews = new List<View>();

                foreach (var anotherView in views.Except(new[] { view }))
                {
                    var regex = new Regex(string.Format(@" (\[?{0}]?\.)?\[?{1}]? ", anotherView.SchemaName, anotherView.Name), RegexOptions.IgnoreCase);
                    if (regex.IsMatch(view.Definition))
                        view.ParentViews.Add(anotherView);
                }
            }

            foreach (var view in views)
                FillLevels(view);
        }

        private void FillLevels(View table)
        {
            if (table.Level > table.ParentViews.Select(t => t.Level).DefaultIfEmpty().Max())
                return;

            foreach (var parent in table.ParentViews)
                FillLevels(parent);

            table.Level = table.ParentViews.Select(t => t.Level).DefaultIfEmpty().Max() + 1;
        }
    }
}
