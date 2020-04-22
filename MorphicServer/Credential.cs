namespace MorphicServer
{
    public interface Credential: Record
    {
        string? UserId { get; set; }
    }
}