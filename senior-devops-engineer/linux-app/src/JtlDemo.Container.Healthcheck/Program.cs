var healthEndpoint = new Uri("http://127.0.0.1:8080/healthz");

using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

try
{
    using var response = await client.GetAsync(healthEndpoint);
    return response.IsSuccessStatusCode ? 0 : 1;
}
catch (HttpRequestException)
{
    return 1;
}
catch (TaskCanceledException)
{
    return 1;
}
