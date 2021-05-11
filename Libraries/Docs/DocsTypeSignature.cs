﻿using System.Xml.Linq;

namespace Libraries.Docs
{
    public class DocsTypeSignature
    {
        private readonly XElement XETypeSignature;

        public string Language
        {
            get
            {
                return XmlHelper.GetAttributeValue(XETypeSignature, "Language");
            }
        }

        public string Value
        {
            get
            {
                return XmlHelper.GetAttributeValue(XETypeSignature, "Value");
            }
        }

        public DocsTypeSignature(XElement xeTypeSignature)
        {
            XETypeSignature = xeTypeSignature;
        }
    }
}