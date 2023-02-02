using System;

namespace LogGrinder.Models
{
    [Serializable]
    public class CancelWithResultException : OperationCanceledException
    {
        public object? Result { get;}
        public CancelWithResultException() { }
        public CancelWithResultException(string message) : base(message) { }
        public CancelWithResultException(string message, Exception inner) : base(message, inner) { }

        public CancelWithResultException(object result) => Result = result;

        public CancelWithResultException(string message, object result) : base(message) => Result = result;

        public CancelWithResultException(string message, Exception inner, object result) : base(message, inner) => Result = result;
    }
}
