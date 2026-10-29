namespace LabCalculator.Core.Dependencies;

/// <summary>Граф залежностей між клітинками: перевіряє цикли і визначає порядок перерахунку.</summary>
public class DependencyGraph
{
	// Клітинка → клітинки, на які вона посилається.
	private readonly Dictionary<string, HashSet<string>> _dependsOn = new();

	// Клітинка → клітинки, що посилаються на неї.
	private readonly Dictionary<string, HashSet<string>> _usedBy = new();

	public void SetDependencies(string cell, IEnumerable<string> references)
	{
		RemoveOutgoingEdges(cell);

		var newReferences = new HashSet<string>(references);
		if (newReferences.Count == 0)
		{
			return;
		}

		_dependsOn[cell] = newReferences;
		foreach (string reference in newReferences)
		{
			bool hasUsers = _usedBy.TryGetValue(reference, out HashSet<string>? users);
			if (!hasUsers)
			{
				users = new HashSet<string>();
				_usedBy[reference] = users;
			}

			users!.Add(cell);
		}
	}

	/// <summary>Чи з'явиться цикл, якщо клітинка cell посилатиметься на references.</summary>
	public bool WouldCreateCycle(string cell, IEnumerable<string> references)
	{
		// Якщо на cell ніхто не посилається, цикл можливий лише через посилання на саму себе.
		bool isUsed = _usedBy.ContainsKey(cell);
		if (!isUsed)
		{
			return references.Contains(cell);
		}

		var visited = new HashSet<string>();
		var stack = new Stack<string>(references);

		while (stack.Count > 0)
		{
			string current = stack.Pop();
			if (current == cell)
			{
				return true;
			}

			bool isFirstVisit = visited.Add(current);
			if (!isFirstVisit)
			{
				continue;
			}

			bool hasReferences = _dependsOn.TryGetValue(current, out HashSet<string>? next);
			if (hasReferences)
			{
				foreach (string reference in next!)
				{
					stack.Push(reference);
				}
			}
		}

		return false;
	}

	public IReadOnlyList<string> GetCellsToRecalculate(string cell)
		=> GetEvaluationOrder(CollectDependents(cell));

	/// <summary>Упорядковує клітинки так, щоб кожна йшла після тих із них, від яких залежить.</summary>
	public IReadOnlyList<string> GetEvaluationOrder(IEnumerable<string> cells)
	{
		var selected = new HashSet<string>(cells);

		// Топологічне сортування (алгоритм Кана): скільки залежностей кожної клітинки ще не оброблено.
		var remaining = new Dictionary<string, int>();
		foreach (string current in selected)
		{
			remaining[current] = _dependsOn.TryGetValue(current, out HashSet<string>? references)
				? references.Count(selected.Contains)
				: 0;
		}

		var ready = new SortedSet<string>(
			remaining.Where(pair => pair.Value == 0).Select(pair => pair.Key),
			StringComparer.Ordinal);

		var order = new List<string>();
		while (ready.Count > 0)
		{
			string current = ready.Min!;
			ready.Remove(current);
			order.Add(current);

			bool hasUsers = _usedBy.TryGetValue(current, out HashSet<string>? users);
			if (!hasUsers)
			{
				continue;
			}

			foreach (string user in users!)
			{
				bool isSelected = selected.Contains(user);
				if (!isSelected)
				{
					continue;
				}

				remaining[user]--;
				if (remaining[user] == 0)
				{
					ready.Add(user);
				}
			}
		}

		return order;
	}

	private void RemoveOutgoingEdges(string cell)
	{
		bool hadReferences = _dependsOn.Remove(cell, out HashSet<string>? oldReferences);
		if (!hadReferences)
		{
			return;
		}

		foreach (string reference in oldReferences!)
		{
			bool hasUsers = _usedBy.TryGetValue(reference, out HashSet<string>? users);
			if (hasUsers)
			{
				users!.Remove(cell);
				if (users.Count == 0)
				{
					_usedBy.Remove(reference);
				}
			}
		}
	}

	private HashSet<string> CollectDependents(string cell)
	{
		var result = new HashSet<string>();
		var stack = new Stack<string>();
		stack.Push(cell);

		while (stack.Count > 0)
		{
			string current = stack.Pop();
			bool hasUsers = _usedBy.TryGetValue(current, out HashSet<string>? users);
			if (!hasUsers)
			{
				continue;
			}

			foreach (string user in users!)
			{
				if (user == cell)
				{
					continue;
				}

				bool isNewDependent = result.Add(user);
				if (isNewDependent)
				{
					stack.Push(user);
				}
			}
		}

		return result;
	}
}
