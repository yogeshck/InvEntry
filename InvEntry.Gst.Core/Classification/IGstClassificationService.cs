using InvEntry.GST.Models;

namespace InvEntry.GST.Classification
{
    public interface IGstClassificationService
    {
        GstClassificationResult Classify(
            GstClassificationRequest request);
    }
}