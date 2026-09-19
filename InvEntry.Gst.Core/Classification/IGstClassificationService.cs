using InvEntry.Gst.Core.Models;

namespace InvEntry.Gst.Core.Classification
{
    public interface IGstClassificationService
    {
        GstClassificationResult Classify(
            GstClassificationRequest request);
    }
}