using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeInspectorNamespace
{
	[RequireComponent( typeof( ScrollRect ) )]
	public class RecycledListView : MonoBehaviour
	{
		// Cached components
		[SerializeField]
		private RectTransform viewportTransform;
		[SerializeField]
		private RectTransform contentTransform;

		private float itemHeight, _1OverItemHeight;
		private float viewportHeight;

		private Dictionary<int, RecycledListItem> items = new Dictionary<int, RecycledListItem>();
		private Stack<RecycledListItem> pooledItems = new Stack<RecycledListItem>();

		IListViewAdapter adapter = null;

		private bool isDirty = false;

		// Current indices of items shown on screen
		private int currentTopIndex = -1, currentBottomIndex = -1;

		void Start()
		{
			GetComponent<ScrollRect>().onValueChanged.AddListener( ( pos ) => UpdateItemsInTheList() );
		}

		void Update()
		{
			if( isDirty )
			{
				viewportHeight = viewportTransform.rect.height;
				UpdateItemsInTheList();

				isDirty = false;
			}
		}

		public void SetAdapter( IListViewAdapter adapter )
		{
			this.adapter = adapter;

			itemHeight = adapter.ItemHeight;
			_1OverItemHeight = 1f / itemHeight;
		}

		// Update the list
		public void UpdateList()
		{
			contentTransform.anchoredPosition = Vector2.zero;

            float newHeight = Mathf.Max( 1f, adapter.Count * itemHeight );
			contentTransform.sizeDelta = new Vector2( 0f, newHeight );
			viewportHeight = viewportTransform.rect.height;

			UpdateItemsInTheList( true );
		}

		public void ResetList()
		{
			itemHeight = adapter.ItemHeight;
			_1OverItemHeight = 1f / itemHeight;

			if( currentTopIndex > -1 && currentBottomIndex > -1 )
			{
				if( currentBottomIndex > adapter.Count - 1 )
					currentBottomIndex = adapter.Count - 1;

				DestroyItemsBetweenIndices( currentTopIndex, currentBottomIndex );

				currentTopIndex = -1;
				currentBottomIndex = -1;
			}

			UpdateList();
		}

		// Window is resized, update the list
		private void OnRectTransformDimensionsChange()
		{
			isDirty = true;
		}

		// Calculate the indices of items to show
		private void UpdateItemsInTheList( bool updateAllVisibleItems = false )
		{
			if( adapter == null )
				return;

			// If there is at least one item to show
			if( adapter.Count > 0 )
			{
				float contentPos = contentTransform.anchoredPosition.y - 1f;
				
				int newTopIndex = (int) ( contentPos * _1OverItemHeight );
				int newBottomIndex = (int) ( ( contentPos + viewportHeight + 2f ) * _1OverItemHeight );

				if( newTopIndex < 0 )
					newTopIndex = 0;

				if( newBottomIndex > adapter.Count - 1 )
					newBottomIndex = adapter.Count - 1;

				if( currentTopIndex == -1 )
				{
					// There are no active items

					updateAllVisibleItems = true;

					currentTopIndex = newTopIndex;
					currentBottomIndex = newBottomIndex;

					CreateItemsBetweenIndices( newTopIndex, newBottomIndex );
				}
				else
				{
					// There are some active items

					if( newBottomIndex < currentTopIndex || newTopIndex > currentBottomIndex )
					{
						// If user scrolled a lot such that, none of the items are now within
						// the bounds of the scroll view, pool all the previous items and create
						// new items for the new list of visible entries
						updateAllVisibleItems = true;

						DestroyItemsBetweenIndices( currentTopIndex, currentBottomIndex );
						CreateItemsBetweenIndices( newTopIndex, newBottomIndex );
					}
					else
					{
						// User did not scroll a lot such that, some items are are still within
						// the bounds of the scroll view. Don't destroy them but update their content,
						// if necessary
						if( newTopIndex > currentTopIndex )
						{
							DestroyItemsBetweenIndices( currentTopIndex, newTopIndex - 1 );
						}

						if( newBottomIndex < currentBottomIndex )
						{
							DestroyItemsBetweenIndices( newBottomIndex + 1, currentBottomIndex );
						}

						if( newTopIndex < currentTopIndex )
						{
							CreateItemsBetweenIndices( newTopIndex, currentTopIndex - 1 );

							// If it is not necessary to update all the items,
							// then just update the newly created items. Otherwise,
							// wait for the major update
							if( !updateAllVisibleItems )
							{
								UpdateItemContentsBetweenIndices( newTopIndex, currentTopIndex - 1 );
							}
						}

						if( newBottomIndex > currentBottomIndex )
						{
							CreateItemsBetweenIndices( currentBottomIndex + 1, newBottomIndex );

							// If it is not necessary to update all the items,
							// then just update the newly created items. Otherwise,
							// wait for the major update
							if( !updateAllVisibleItems )
							{
								UpdateItemContentsBetweenIndices( currentBottomIndex + 1, newBottomIndex );
							}
						}
					}

					currentTopIndex = newTopIndex;
					currentBottomIndex = newBottomIndex;
				}

				if( updateAllVisibleItems )
				{
					// Update all the items
					UpdateItemContentsBetweenIndices( currentTopIndex, currentBottomIndex );
				}
			}
			else if( currentTopIndex != -1 )
			{
				// There is nothing to show but some items are still visible; pool them
				DestroyItemsBetweenIndices( currentTopIndex, currentBottomIndex );

				currentTopIndex = -1;
			}
		}

		private void CreateItemsBetweenIndices( int topIndex, int bottomIndex )
		{
			for( int i = topIndex; i <= bottomIndex; i++ )
			{
				CreateItemAtIndex( i );
			}
		}

		// Create (or unpool) an item
		private void CreateItemAtIndex( int index )
		{
			RecycledListItem item;
			if( pooledItems.Count > 0 )
			{
				item = pooledItems.Pop();
				item.gameObject.SetActive( true );
			}
			else
			{
				item = adapter.CreateItem( contentTransform );
				item.SetAdapter( adapter );
			}

			// Reposition the item
			( (RectTransform) item.transform ).anchoredPosition = new Vector2( 0f, -index * itemHeight );

			// To access this item easily in the future, add it to the dictionary
			items[index] = item;
		}

		private void DestroyItemsBetweenIndices( int topIndex, int bottomIndex )
		{
			for( int i = topIndex; i <= bottomIndex; i++ )
			{
				RecycledListItem item = items[i];

				item.gameObject.SetActive( false );
				pooledItems.Push( item );
			}
		}

		private void UpdateItemContentsBetweenIndices( int topIndex, int bottomIndex )
		{
			for( int i = topIndex; i <= bottomIndex; i++ )
			{
				RecycledListItem item = items[i];

				item.Position = i;
				adapter.SetItemContent( item );
			}
		}
	}
}