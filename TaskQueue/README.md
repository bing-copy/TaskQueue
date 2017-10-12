## Usage
1. Define your own tasks.
```
public class ListCrawler : ScheduledTask<ListCrawlerTaskData>
{
	private readonly string _listUrlTpl;
	private readonly string _itemSelector;
	private readonly string _domain;
	private readonly HttpClient _httpClient = new HttpClient();

	public ListCrawler(string listUrlTpl, string itemSelector, string domain)
	{
		_listUrlTpl = listUrlTpl;
		_itemSelector = itemSelector;
		_domain = domain;
	}

	public override object State { get; }
	protected override async Task<List<object>> ExecuteAsync(ListCrawlerTaskData data)
	{
		var listRsp = await _httpClient.GetAsync(string.Format(_listUrlTpl, data.Page));
		var cQ = new CQ(await listRsp.Content.ReadAsStringAsync());
		var items = cQ[_itemSelector].ToList();
			
	}
}
```