﻿using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsParameter
    {
        private readonly XElement XEParameter;
        public string Name
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEParameter, "Name");
            }
        }
        public string Type
        {
            get
            {
                return XmlHelper.GetAttributeValue(XEParameter, "Type");
            }
        }
        public DocsParameter(XElement xeParameter)
        {
            XEParameter = xeParameter;
        }
    }
}