namespace Polly_Test
{
    public class QueryOptions
    {
        /// <summary>
        /// Разрешет использовать локальные ip
        /// </summary>
        public bool UseLocalIp { get; set; }

        /// <summary>
        /// Включает буферизацию данных в IpCahnger перед отдачей контента
        /// </summary>
        public bool EnableBuffering { get; set; }

        public int MaxRedirectCount { get; set; } = 5;
    }
}
