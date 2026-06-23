namespace SmartPulseEpiasApp;

public class TransactionSummaryService
{
    public List<ContractSummary> BuildSummaries(List<TransactionHistoryItem> items)
    {
        var byContract = new Dictionary<string, List<TransactionHistoryItem>>();

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.ContractName))
            {
                continue;
            }

            if (!byContract.TryGetValue(item.ContractName, out var group))
            {
                group = new List<TransactionHistoryItem>();
                byContract[item.ContractName] = group;
            }

            group.Add(item);
        }

        var summaries = new List<ContractSummary>();

        foreach (var entry in byContract)
        {
            var contractName = entry.Key;
            var group = entry.Value;

            decimal totalAmount = 0m;
            decimal totalQuantity = 0m;

            foreach (var transaction in group)
            {
                totalAmount += (transaction.Price * transaction.Quantity) / 10m;
                totalQuantity += transaction.Quantity / 10m;
            }

            summaries.Add(new ContractSummary
            {
                ContractName = contractName,
                ContractDateTime = ParseContractDateTime(contractName),
                TotalTransactionAmount = totalAmount,
                TotalTransactionQuantity = totalQuantity,
                WeightedAveragePrice = totalQuantity != 0m ? totalAmount / totalQuantity : 0m
            });
        }

        summaries.Sort((a, b) => a.ContractDateTime.CompareTo(b.ContractDateTime));
        return summaries;
    }

    private DateTime ParseContractDateTime(string contractName)
    {
        if (string.IsNullOrWhiteSpace(contractName))
        {
            throw new ArgumentException($"Invalid contractName: {contractName}");
        }

        if (contractName.Length < 10)
        {
            throw new ArgumentException($"Invalid contractName: {contractName}");
        }

        if (!contractName.StartsWith("PH", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Expected contract to start with PH: {contractName}");
        }

        var digits = contractName.Substring(2, 8);
        var yy = int.Parse(digits.Substring(0, 2));
        var month = int.Parse(digits.Substring(2, 2));
        var day = int.Parse(digits.Substring(4, 2));
        var hour = int.Parse(digits.Substring(6, 2));
        var year = 2000 + yy;

        return new DateTime(year, month, day, hour, 0, 0);
    }
}
