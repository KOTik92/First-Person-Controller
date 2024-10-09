using System;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        public string _BaseCondition
        {
            get { return mBaseCondition; }
        }
        
        public int _ExecutionNumber
        {
            get { return mExecutionNumber; }
        }

        private string mBaseCondition = String.Empty;
        private int mExecutionNumber = 1;

        public ShowIfAttribute(string baseCondition, int executionNumber = 1)
        {
            mBaseCondition = baseCondition;
            mExecutionNumber = executionNumber;
        }
    }
}
