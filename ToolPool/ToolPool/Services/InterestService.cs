using ToolPool.Models;

public class InterestService
{
    public Task<InterestResponse> SubmitInterestAsync(InterestRequest request)
    {
        // create chat here
        return Task.FromResult(new InterestResponse
        {
            ChannelUrl = Guid.NewGuid().ToString()
        });
    }
}