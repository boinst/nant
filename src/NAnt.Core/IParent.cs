namespace NAnt.Core
{
    /// <summary>
    /// A Parent of an Element (a Task or a Target) is the containing <see cref="Task" />, <see cref="Target" />, or 
    /// <see cref="Project" />, depending on where the element is defined.
    /// </summary>
    public interface IParent
    {
        /// <summary>
        /// </summary>
        void Log(BuildEventArgs buildEvent);
    }
}