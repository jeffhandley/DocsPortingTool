namespace LeadingTriviaTestData.Directives.Expected
{
    /// <summary>Directives</summary>
    /// <remarks>Directives</remarks>
#if false
    internal
#else
    public
#endif
    class MyType
    {
        #region MyEnum

        /// <summary>Directives</summary>
        /// <remarks>Directives</remarks>
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
        /// <summary>Directives</summary>
        /// <remarks>Directives</remarks>
        public int MyField;
#pragma warning restore

#nullable enable
        /// <summary>Directives</summary>
        /// <remarks>Directives</remarks>
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

        /// <summary>Directives</summary>
        /// <remarks>Directives</remarks>
#if true
        public bool MyMethod()
        {
            return true;
        }
#endif
    }
}