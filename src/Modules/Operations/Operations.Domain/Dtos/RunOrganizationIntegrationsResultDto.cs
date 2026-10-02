namespace Operations.Domain.Dtos
{
    public class RunOrganizationIntegrationsResultDto
    {
        /// <summary>Configurations that were run and succeeded.</summary>
        public int Succeeded { get; set; }

        /// <summary>Configurations that were run and failed; their error is on the configuration's run history.</summary>
        public int Failed { get; set; }

        /// <summary>Configurations left alone because they ran moments ago or are running now.</summary>
        public int Skipped { get; set; }
    }
}
