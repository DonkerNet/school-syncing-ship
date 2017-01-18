namespace SyncingShip.Protocol
{
    public enum SyncStatusCode
    {
        Ok = 200,
        BadRequest = 400,
        NotFound = 404,
        FileConflict = 412,
        //RequestEntityTooLarge = 413,
        InternalServerError = 500
    }
}