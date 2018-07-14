namespace TheTimeApp.TimeData
{
    public class SqlMode
    {
        public bool Read { get; set; } = false;
        public bool Write { get; set; } = false;
        public bool ReadWrite => Read && Write;
    }
}