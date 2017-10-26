using System;

namespace Common
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
    public class RunOrderAttribute : Attribute
    {
        //RunOrderAttribute helps determine the sequence for validation
        //Add this attribute to the class that implements validation interface
        //if you care about the order it runs. Otherwise, it will be added to the end 
        public RunOrderAttribute(int order)
        {
            this.Order = order;
        }

        public int Order { get; set; }
    }
}
