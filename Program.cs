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

var conDict = new Dictionary<int, List<int>>();

for (int i = startLine; i < lines.Length; i++)
{
	if (lines[i] == string.Empty) break;
	var lssplit = lines[i].Trim().Split(' ');
	var lsplit = lssplit.Where(v => v != string.Empty).Select(v => v.Trim()).ToList();

	int lastProcessedASN = -1;

	if (lssplit[lssplit.Length - 1] != "i" && lssplit[lssplit.Length - 1] != "?")
		continue;

	for (int j = lssplit.Length - 2; j > 0; j--)
	{
		if (
			lssplit[j] == string.Empty ||
			!int.TryParse(lssplit[j], out int asn)
		) {
			if (localAsn == null)
				break;
			
			int lasn = localAsn.Value;

			if (!conDict.ContainsKey(lasn))
				conDict.Add(lasn, new List<int>());

			if (lastProcessedASN != -1)
			{
				if (!conDict[lasn].Contains(lastProcessedASN))
					conDict[lasn].Add(lastProcessedASN);
				if (!conDict[lastProcessedASN].Contains(lasn))
					conDict[lastProcessedASN].Add(lasn);
			}

			break;
		}

		//Console.Write($"{asn} ");

		if (!conDict.ContainsKey(asn))
			conDict.Add(asn, new List<int>());

		if (lastProcessedASN != -1)
		{
			if (!conDict[asn].Contains(lastProcessedASN))
				conDict[asn].Add(lastProcessedASN);
			if (!conDict[lastProcessedASN].Contains(asn))
				conDict[lastProcessedASN].Add(asn);
		}

		lastProcessedASN = asn;
	}

	//Console.WriteLine();
	//Console.WriteLine(lines[i]);
}

List<(int, int)> connectedLine = new();
List<string> mermaidStr = new() { 
"<DOCTYPE html>",
"<html><head><title>BGP Peers</title></head><body><pre class=\"mermaid\">",
"graph LR"
};

foreach(var con in conDict.OrderBy(v => v.Value.Distinct().Count()))
{
	Console.Write($"{con.Key}: ");
	foreach(var cnct in con.Value.Distinct().OrderByDescending(v => conDict[v].Count))
	{
		if (cnct == con.Key) continue;
		Console.Write($"{cnct} ");
		if (!connectedLine.Contains((cnct, con.Key)))
		{
			mermaidStr.Add($"{con.Key} --- {cnct}");
			connectedLine.Add((con.Key, cnct));
		}
	}
	Console.WriteLine();
}

mermaidStr.Add("</pre><script type=\"module\">import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.esm.min.mjs';</script></body></html>");

File.WriteAllLines(outputFile, mermaidStr.ToArray());