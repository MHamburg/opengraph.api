using System;

namespace OpenGraph
{
    /// <summary>
    /// An invalid specification exception
    /// </summary>
    [Serializable]
    public class InvalidSpecificationException : Exception
    {
        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidSpecificationException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public InvalidSpecificationException(string message)
            : base(message)
        {
        }

        #endregion Public Constructors
    }
}