using System.Collections.Generic;

namespace DbHierarchyGenerator.Models
{
    public abstract class Definable : DbObject
    {
        public string Definition { get; set; }

        protected Definable(string schemaName, string name, string definition) : base(schemaName, name)
        {
            Definition = definition;
        }
    }

    public class Function : Definable
    {
        public Function(string schemaName, string name, string definition)
            : base(schemaName, name, definition)
        {
        }

        public override string ShortNames
        {
            get { return "'FN', 'TF'"; }
        }
    }

    public class Procedure : Definable
    {
        public Procedure(string schemaName, string name, string definition) : base(schemaName, name, definition)
        {
        }

        public override string ShortNames
        {
            get { return "'P', 'PC'"; }
        }
    }

    public class View : Definable
    {
        public List<View> ParentViews { get; set; }

        public uint Level { get; set; }

        public View(string schemaName, string name, string definition)
            : base(schemaName, name, definition)
        {
        }

        public override string ShortNames
        {
            get { return "'V'"; }
        }
    }

    public class Trigger : Definable
    {
        public string ParentSchemaName { get; set; }
        public string ParentName { get; set; }

        public Trigger(string schemaName, string name, string definition, string parentSchemaName, string parentName)
            : base(schemaName, name, definition)
        {
            ParentSchemaName = parentSchemaName;
            ParentName = parentName;
        }

        public override string ShortNames
        {
            get { return "'TR'"; }
        }
    }
}
