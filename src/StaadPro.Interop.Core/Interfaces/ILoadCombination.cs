using StaadPro.Interop.Core.Common;
namespace StaadPro.Interop.Core.Interfaces
{
    /// <summary>Identifies a detached load combination and its literal title.</summary>
    public interface ILoadCombination : IEntity
    {
        /// <summary>Gets the stored combination title.</summary>
        /// <value>The text supplied to the constructor; query readers preserve native text exactly.</value>
        string Title { get; }
    }
}
