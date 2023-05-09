using System.Text.RegularExpressions;

string inputFile = "sample#sh ip bgp all.txt";
string outputFile = "peers.html";
int? localAsn = null;

var options = new Mono.Options.OptionSet { 
	{ "i|inputFile=", "inputFile", v => inputFile = v }, 
	{ "O|outputFile=", "outputFile", v=> outputFile = v }, 
	{ "a|localAsn=", "local ASN", (int v)=> localAsn = v }, 
};
options.Parse (args);

var lines = File.ReadAllLines(inputFile);

int startLine = 0;

for (int i = 0; i < lines.Length; i++)
{
	var line = lines[i];

	if (localAsn == null && Regex.IsMatch(line, @"^\s*Local AS number\s+\d+\s*$"))
	{
		if (int.TryParse(Regex.Replace(line, @"^\s*Local AS number\s+(\d+)\s*$", "$1"), out int v))
			localAsn = v;
		continue;
	}

	var lsplit = line.Split(' ').Where(v => v != string.Empty).Select(v => v.Trim()).ToList();
	if (lsplit.Count > 2 && lsplit[0] == "Network" && lsplit[1] == "Next" && lsplit[2] == "Hop")
	{
		startLine = i + 1;
		break;
	}
}

var asTotalCons = new List<List<int>>();

for (int i = startLine; i < lines.Length; i++)
{
	if (lines[i] == string.Empty) break;
	var lssplit = lines[i].Trim().Split(' ');
	var lsplit = lssplit.Where(v => v != string.Empty).Select(v => v.Trim()).ToList();

	if (lssplit[lssplit.Length - 1] != "i" && lssplit[lssplit.Length - 1] != "?")
		continue;

	List<int> asCon = new();

	for (int j = lssplit.Length - 2; j > 0; j--)
	{
		if (
			lssplit[j] == string.Empty ||
			!int.TryParse(lssplit[j], out int asn)
		) {
			break;
		}

		//Console.Write($"{asn} ");

		asCon.Add(asn);
	}

	if (localAsn != null)
		asCon.Add(localAsn.Value);

	asCon.Reverse();
	asTotalCons.Add(asCon.Distinct().ToList());

	//Console.WriteLine();
	//Console.WriteLine(lines[i]);
}

List<(int, int)> connectedLine = new();
List<string> mermaidStr = new() { 
"<DOCTYPE html>",
"<html><head><title>BGP Peers</title></head><body><pre class=\"mermaid\">",
"graph LR"
};

var simplifiedPeerList = new List<List<int>>();

foreach(var con in asTotalCons.OrderByDescending(v => v.Count))
{
	bool add = true;
	foreach (var sp in simplifiedPeerList)
	{
		if (
			(con.Count > sp.Count) ||
			(con.Count == sp.Count && con.SequenceEqual(sp)) ||
			(con.SequenceEqual(sp.Take(con.Count())))
		) {
			add = false;
			break;
		}	
	}
	if (add)
	{
		simplifiedPeerList.Add(con);
		Console.WriteLine($"Added {con.Count} length path.");
	}
}

foreach(var con in simplifiedPeerList)
{
	for (int i = 0; i < con.Count - 1; i++)
	{
		int conFrom = con[i];
		int conTo = con[i + 1];
		Console.Write($"{conFrom} ");
		if (
			!connectedLine.Contains((conFrom, conTo)) &&
			!connectedLine.Contains((conTo, conFrom))
		) {
			mermaidStr.Add($"{conFrom} --- {conTo}");
			connectedLine.Add((conFrom, conTo));
		}
	}
	Console.WriteLine(con[con.Count - 1]);
}

mermaidStr.Add("</pre><script type=\"module\">import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.esm.min.mjs';</script></body></html>");

File.WriteAllLines(outputFile, mermaidStr.ToArray());