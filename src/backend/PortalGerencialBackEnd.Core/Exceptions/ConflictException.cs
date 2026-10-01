namespace PortalGerencialBackEnd.Core.Exceptions
{
    /// <summary>Conflicto de estado (409), p. ej. pedir una corrida del job mientras otra está en curso.</summary>
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}
