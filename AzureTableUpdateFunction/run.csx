#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public static void Run(TimerInfo myTimer, CloudTable existingMetricsTable, ICollector<MetricsRow> metricsTable, TraceWriter log)
{
	//SOURCE CONTROL UPDATE
    TrelloSettings settings = new TrelloSettings("d642c79f38bb2eef7e51454bcd25d62a8711becb8a38aa18f81504ba4a4a6884", "7809f5ecffcd98271e7e0d0f97256ffc", "");
	List<Card> backlogCards = new TrelloCards(settings).GetAllCardsForAList("54f88861ec56b782d05202f7");
	List<Card> toDoCards = new TrelloCards(settings).GetAllCardsForAList("543d3dd473131a2450786b51");
	List<Card> inProgressCards = new TrelloCards(settings).GetAllCardsForAList("543d3dd473131a2450786b52");
	List<Card> readyForTestCards = new TrelloCards(settings).GetAllCardsForAList("543d3dd473131a2450786b53");
	List<Card> TestingCards = new TrelloCards(settings).GetAllCardsForAList("543d3df626d4237bf9a84753");
	List<Card> ReadyForClientCards = new TrelloCards(settings).GetAllCardsForAList("543d3e2c2836b4aa287925b2");
	List<Card> ClientQaCards = new TrelloCards(settings).GetAllCardsForAList("543d3e447526cdbb1393cb6e");
	List<Card> ReadyForReleaseCards = new TrelloCards(settings).GetAllCardsForAList("543d3e4d1a775ce22790d498");
	List<Card> ReleasedCards = new TrelloCards(settings).GetAllCardsForAList("543d3e56af5d203290588360");

	var dateTimeKey = DateTime.Now.ToString("yyyyMMdd");

	TableOperation operation = TableOperation.Retrieve<MetricsRow>("Metrics", dateTimeKey);
    TableResult result = existingMetricsTable.Execute(operation);
    MetricsRow metricsRow = (MetricsRow)result.Result;

    if (metricsRow != null)
	{
		log.Verbose($"{metricsRow.Date} is {metricsRow.Backlog}");

	    metricsRow.Backlog = backlogCards.Sum(c => c.Complexity);
        metricsRow.ToDo = toDoCards.Sum(c => c.Complexity);
        metricsRow.Inprogress = inProgressCards.Sum(c => c.Complexity);
        metricsRow.ReadyForTest = readyForTestCards.Sum(c => c.Complexity);
        metricsRow.Testing = TestingCards.Sum(c => c.Complexity);
        metricsRow.ReadyForClient = ReadyForClientCards.Sum(c => c.Complexity);
        metricsRow.ClientQA = ClientQaCards.Sum(c => c.Complexity);
        metricsRow.ReadyForRelease = ReadyForReleaseCards.Sum(c => c.Complexity);
        metricsRow.Released = ReleasedCards.Sum(c => c.Complexity);

	    operation = TableOperation.Replace(metricsRow);
	    existingMetricsTable.Execute(operation);
	}
	else
	{
	    metricsTable.Add(
                new MetricsRow() {
                    PartitionKey = "Metrics",
                    RowKey = dateTimeKey,
                    Date = DateTime.Now.ToShortDateString(),
                    Backlog = backlogCards.Sum(c => c.Complexity),
                    ToDo = toDoCards.Sum(c => c.Complexity),
                    Inprogress = inProgressCards.Sum(c => c.Complexity),
                    ReadyForTest = readyForTestCards.Sum(c => c.Complexity),
                    Testing = TestingCards.Sum(c => c.Complexity),
                    ReadyForClient = ReadyForClientCards.Sum(c => c.Complexity),
                    ClientQA = ClientQaCards.Sum(c => c.Complexity),
                    ReadyForRelease = ReadyForReleaseCards.Sum(c => c.Complexity),
                    Released = ReleasedCards.Sum(c => c.Complexity)
                    }
                );
	}
}

public class TrelloCards
{
	private readonly string _authToken;
	private readonly string _trelloKey;
	private readonly string _trelloUrl;

	public TrelloCards(TrelloSettings trelloSettings)
	{
		_trelloKey = trelloSettings.ApiKey;
		_authToken = trelloSettings.AuthToken;
		_trelloUrl = trelloSettings.ApiUrl;
	}

    public List<Card> GetAllCardsForAList(string listId)
	{
		var url = string.Format("https://api.trello.com/1/lists/{0}/cards?card_fields=name&fields=name,labels&key={2}&token={1}", listId, _authToken, _trelloKey, _trelloUrl);
		var response = GetResponse(url);
		return JsonConvert.DeserializeObject<List<Card>>(response);
	}
}

private static string GetResponse(string url)
{
	string response;

	using (var webClient = new WebClient())
	{
		response = webClient.DownloadString(url);
	}
	return response;
}


public class Card
{
	public Card(string id, string name)
	{
		Id = id;
		Name = name;
	}

	public string Id { get; private set; }
	public string Name { get; private set; }
	public string IdList { get; set; }

	public DateTime Released { get; set; }
	public DateTime InProgress { get; set; }
	public DateTime ClientTest { get; set; }
	public double Complexity {
		get
		{
			var match = Regex.Match(Name, @"^\((\d*\.)?\d+?\)");
			var value = "0";
			if (!string.IsNullOrEmpty(match.Value))
			{
				value = match.Value;
			}
			match = Regex.Match(Name, @"\((\d*\.)?\d+?\)");
			if (!string.IsNullOrEmpty(match.Value))
			{
				value = match.Value;
			}
			//match = Regex.Match(Name, @"\(\d+\)");
			//if (!string.IsNullOrEmpty(match.Value))
			//{
			//	value = match.Value;
			//}
			return Convert.ToDouble(value.Replace("(","").Replace(")",""));
		}
	}

	public DateTime ReadyForRelease { get; set; }

	public List<Label> Labels { get; set; }
}

public class Label
{
	public Label(string id, string name)
	{
		Id = id;
		Name = name	;
	}

	public string Id { get; private set; }
	public string Name { get; private set; }

}

/*Models*/

public class MetricsRow : TableEntity
{
    public string Date { get; set; }
    public double Backlog { get; set; }
    public double ToDo { get; set; }
    public double Inprogress { get; set; }
    public double ReadyForTest { get; set; }
    public double Testing { get; set; }
    public double ReadyForClient { get; set; }
    public double ClientQA { get; set; }
    public double ReadyForRelease { get; set; }
    public double Released { get; set; }
}

public class TrelloSettings
{
	public TrelloSettings(string authToken, string apiKey, string apiUrl)
	{
		AuthToken = authToken;
		ApiKey = apiKey;
		ApiUrl = apiUrl;
	}

	public string AuthToken { get; private set; }
	public string ApiKey { get; private set; }
	public string ApiUrl { get; private set; }
}
