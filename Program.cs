var lines = File.ReadAllLines("sample#sh ip bgp all.txt");

int startLine = 0;

for (int i = 0; i < lines.Length; i++)
{
	var line = lines[i];
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
	var lsplit = lines[i].Split(' ').Where(v => v != string.Empty).Select(v => v.Trim()).ToList();

	int lastProcessedASN = -1;

	if (lsplit.Count < 3) continue;

	int startPos = 3;
	if (!lsplit[1].Contains("/")) startPos--;

	for (int j = startPos; j < lsplit.Count; j++)
	{
		if (lsplit[j] == "0") continue;
		if (!int.TryParse(lsplit[j], out int asn)) break;
		//Console.Write($"{asn} ");

		if (!conDict.ContainsKey(asn))
			conDict.Add(asn, new List<int>());

		if (lastProcessedASN != -1)
		{
			conDict[asn].Add(lastProcessedASN);
			conDict[lastProcessedASN].Add(asn);
		}

		lastProcessedASN = asn;
	}

	//Console.WriteLine();
	//Console.WriteLine(lines[i]);
}

List<(int, int)> connectedLine = new();
List<string> mermaidStr = new();

foreach(var con in conDict)
{
	Console.Write($"{con.Key}: ");
	foreach(var cnct in con.Value.Distinct())
	{
		Console.Write($"{cnct} ");
		if (!connectedLine.Contains((cnct, con.Key)))
		{
			mermaidStr.Add($"{con.Key} --- {cnct}");
			connectedLine.Add((con.Key, cnct));
		}
	}
	Console.WriteLine();
}

File.WriteAllLines("mermaid.txt", mermaidStr.ToArray());