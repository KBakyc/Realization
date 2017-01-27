namespace DataObjects
{
    public interface ITrackable
    {
        TrackingInfo TrackingState { get; set; }
    }
}