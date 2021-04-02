namespace LeadingTriviaTestData.Directives.Original
{
#if false
    internal
#else
    public
#endif
    class MyType
    {
        #region MyEnum

#if true
        public
#else
        internal
#endif
        enum MyEnum
        {
            FirstValue = 1,
            SecondValue,
            ThirdValue,
        }

        #endregion

#pragma warning disable
        public int MyField;
#pragma warning restore

#nullable enable
        public string MyProperty
        {
            get
            {
                return "";
            }
            set
            {

            }
        }
#nullable restore

#if true
        public bool MyMethod()
        {
            return true;
        }
#endif
    }
}