namespace SyncingShip.Protocol.ResponseBodies
{
    internal interface IResponseBody
    {
        SyncStatusCode status { get; }
    }
}