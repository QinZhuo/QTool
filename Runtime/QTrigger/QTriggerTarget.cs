using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{
	public class QTriggerTarget : MonoBehaviour
	{
		public GameObject target;
		private void OnTriggerEnter(Collider other)
		{
			target.SendMessage(nameof(OnTriggerEnter), other, SendMessageOptions.DontRequireReceiver);
		}
		private void OnTriggerExit(Collider other)
		{
			target.SendMessage(nameof(OnTriggerExit), other, SendMessageOptions.DontRequireReceiver);
		}
		private void OnTriggerStay(Collider other)
		{
			target.SendMessage(nameof(OnTriggerStay), other, SendMessageOptions.DontRequireReceiver);
		}
		private void OnTriggerEnter2D(Collider2D collision)
		{
			target.SendMessage(nameof(OnTriggerEnter2D), collision, SendMessageOptions.DontRequireReceiver);
		}
		private void OnTriggerExit2D(Collider2D collision)
		{
			target.SendMessage(nameof(OnTriggerExit2D), collision, SendMessageOptions.DontRequireReceiver);
		}
		private void OnTriggerStay2D(Collider2D collision)
		{
			target.SendMessage(nameof(OnTriggerStay2D), collision, SendMessageOptions.DontRequireReceiver);
		}
	}

}
