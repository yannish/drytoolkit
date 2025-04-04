using UnityEngine;

/*
 * TODO:
 * This loads THIS color swatch, but if it's part of a package, it can't be modified.
 * So maybe make this abstract, and try to load up the first non-abstract implementation we find...?
 *
 * 1. first use TypeCache to guarantee that IF there is a non-abstract implementation of ColorSwatches
 *		we've instantiated it.
 */

public abstract class ColorSwatches : ScriptableObject
{

}