﻿using System.Collections.Generic;
using System.Xml.Linq;

namespace Libraries.Docs
{
    public interface IDocsAPI
    {
        public abstract APIKind Kind { get; }
        public abstract bool Changed { get; set; }
        public abstract string FilePath { get; set; }
        public abstract string DocId { get; }
        public abstract XElement Docs { get; }
        public abstract List<DocsParameter> Parameters { get; }
        public abstract List<DocsParam> Params { get; }
        public abstract List<DocsTypeParameter> TypeParameters { get; }
        public abstract List<DocsTypeParam> TypeParams { get; }
        public abstract string Summary { get; set; }
        public abstract string Remarks { get; set; }
        public abstract DocsParam SaveParam(XElement xeCoreFXParam);
        public abstract DocsTypeParam AddTypeParam(string name, string value);
    }
}
