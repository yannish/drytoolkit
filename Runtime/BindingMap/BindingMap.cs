using UnityEngine;

using System;
using System.Linq;
using System.Collections.Generic;

public interface IBindable
{
	System.Collections.IList Entries { get; }
	// List<UnityEngine.Object> forward { get; }
	// List<UnityEngine.Object> backward { get; }
}


[Serializable]
public class BindingMapBase { }


[Serializable]
public class BindingMap<T, U> :
	BindingMapBase,
	ISerializationCallbackReceiver,
	IBindable
{
	public Dictionary<T, U> forwardLookup = new Dictionary<T, U>();
	public Dictionary<U, T> backwardLookup = new Dictionary<U, T>();

	[System.Serializable]
	public class MapEntry
	{
		public T forwardEntry;
		public U backwardEntry;

		public MapEntry() { }

		public MapEntry(T forwardEntry, U backwardEntry)
		{
			this.forwardEntry = forwardEntry;
			this.backwardEntry = backwardEntry;
		}
	}

	[HideInInspector, SerializeField]
	public List<MapEntry> entries = new List<MapEntry>();
	
	public System.Collections.IList Entries => entries;
	
	public List<UnityEngine.Object> forward => entries.Select(t => t.forwardEntry as UnityEngine.Object).ToList();

	public List<UnityEngine.Object> backward => entries.Select(t => t.backwardEntry as UnityEngine.Object).ToList();
	
	public T[] front => forwardLookup.Keys.ToArray();
	
	public U[] back => backwardLookup.Keys.ToArray();


	public void OnBeforeSerialize()
	{
		//Debug.LogWarning("After Serialize on Map");

		entries.Clear();

		foreach (KeyValuePair<T,U> pair in forwardLookup)
		{
			if (pair.Key == null || pair.Value == null)
				continue;

			entries.Add(new MapEntry(pair.Key, pair.Value));
		}
	}

	public void OnAfterDeserialize()
	{
		//Debug.LogWarning("Before Serialize on Map");

		forwardLookup.Clear();
		backwardLookup.Clear();

		foreach (MapEntry entry in entries)
		{
			if (entry.forwardEntry == null || entry.backwardEntry == null)
				continue;

			forwardLookup.Add(entry.forwardEntry, entry.backwardEntry);
			backwardLookup.Add(entry.backwardEntry, entry.forwardEntry);
		}
	}

	public void Clear()
	{
		forwardLookup.Clear();
		backwardLookup.Clear();
	}

	public void Bind(T frontItem, U backItem)
	{
		if(
			!forwardLookup.ContainsKey(frontItem)
			&& !backwardLookup.ContainsKey(backItem)
			)
		{
			forwardLookup.Add(frontItem, backItem);
			backwardLookup.Add(backItem, frontItem);
		}
		else
		{
			Debug.LogWarning("one of these items is already bound");
		}
	}

	public void Rebind(T frontItem, U backItem)
	{
		if (forwardLookup.TryGetValue(frontItem, out var foundBackItem))
		{
			forwardLookup.Remove(frontItem);
			backwardLookup.Remove(foundBackItem);
		}
		
		forwardLookup.Add(frontItem, backItem);
		backwardLookup.Add(backItem, frontItem);
	}

	public void Rebind(U backItem, T frontItem)
	{
		if (backwardLookup.TryGetValue(backItem, out var foundFrontItem))
		{
			forwardLookup.Remove(foundFrontItem);
			backwardLookup.Remove(backItem);
		}
		
		forwardLookup.Add(frontItem, backItem);
		backwardLookup.Add(backItem, frontItem);
	}

	public void Unbind(T frontItem)
	{
		if(forwardLookup.TryGetValue(frontItem, out var foundBackitem))
		{
			forwardLookup.Remove(frontItem);
			backwardLookup.Remove(foundBackitem);
		}
		else
		{
			Debug.LogWarning("tried to unbind frontItem, but it wasn't in forward lookup");
		}
	}

	public void Unbind(U backItem)
	{
		if (backwardLookup.TryGetValue(backItem, out T frontItem))
		{
			forwardLookup.Remove(frontItem);
			backwardLookup.Remove(backItem);
		}
		else
		{
			Debug.LogWarning("tried to unbind backItem, but it wasn't in backward lookup");
		}
	}

	public bool TryGetBinding(T frontItem, out U backItem)
	{
		backItem = default(U);
		forwardLookup.TryGetValue(frontItem, out backItem);
		if (backItem != null)
			return true;
		return false;
	}

	public bool TryGetBinding(U backItem, out T frontItem)
	{
		frontItem = default(T);
		backwardLookup.TryGetValue(backItem, out frontItem);
		if (frontItem != null)
			return true;
		return false;
	}

	public bool CheckIsBound(T frontItem) => forwardLookup.ContainsKey(frontItem);

	public bool CheckIsBound(U backItem) => backwardLookup.ContainsKey(backItem);
}

