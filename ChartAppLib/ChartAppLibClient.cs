namespace ChartAppLib
{
    /// <summary>Validates required environment configuration for ChartAppLib.</summary>
    public static class ChartAppLibClient
    {
        /// <summary>
        /// Validates that the CHARTAPPLIB_API_KEY environment variable is set.
        /// Call this during application startup before using the library.
        /// </summary>
        /// <exception cref="Exception">Thrown when the API key is missing or empty.</exception>
        public static void ValidateApiKey()
        {
            var apiKey = Environment.GetEnvironmentVariable("CHARTAPPLIB_API_KEY");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new Exception("ChartApp API key is missing.");
            }
        }
    }
}
