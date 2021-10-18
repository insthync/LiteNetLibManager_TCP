namespace LiteNetLibManager
{
    public class TcpTransportFactory : BaseTransportFactory
    {
        public override ITransport Build()
        {
            return new TcpTransport();
        }
    }
}
