using System.IO;

namespace DbfDataReader
{
    public abstract class DbfValue<T> : IDbfValue
    {
        protected DbfValue(int length)
        {
            Length = length;
        }

        public int Length { get; }

        public T Value { get; protected set; }

        public abstract void Read(BinaryReader binaryReader);

        public object GetValue()
        {
            return Value;
        }

        public override string ToString()
        {
            if (Value == null)
            {
                return "null";
            }
            return Value.ToString();
        }
    }
}