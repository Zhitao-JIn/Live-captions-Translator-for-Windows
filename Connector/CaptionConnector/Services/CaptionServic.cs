using Grpc.Core;
using CaptionConnector;
using System.Data.Common;

namespace CaptionConnector.Services;

public class CaptionService : Caption.CaptionBase
{
    public override Task<CaptionReply> GetCaption(CaptionRequest request, ServerCallContext context)
    {
        return Task.FromResult(new CaptionReply
        {
            Id = request.Id,
            Message = WinCaption.GetCurrentCaption()
        });
    }
}
