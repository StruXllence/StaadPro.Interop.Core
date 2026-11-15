using StaadPro.Interop.Core.Interfaces;
namespace StaadPro.Interop.Core.Base
{
    /// <summary>Immutable identity and title for detached combination definitions.</summary>
    public abstract class LoadCombination : ILoadCombination
    {
        /// <summary>Stores a combination identity and literal title without native access.</summary>
        /// <param name="id">Combination number stored unchanged.</param>
        /// <param name="title">Title stored unchanged, including null for manual records.</param>
        /// <remarks>Constructors do not validate native existence. Query methods qualify native identity and text.</remarks>
        public LoadCombination(int id, string title) { Id = id; Title = title; }
        /// <summary>Gets the stored combination number.</summary>
        /// <value>The immutable constructor-supplied ID.</value>
        public int Id { get; private set; }
        /// <summary>Gets the stored literal title.</summary>
        /// <value>The immutable constructor-supplied title.</value>
        public string Title { get; private set; }
    }
}
