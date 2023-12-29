import pako from "pako"
import { fromBase64 } from "./cyberChef"

export function deserializePob(encoded: string): XMLDocument {
  const compressed = fromBase64(encoded, "A-Za-z0-9-_=", "byteArray")
  const xmlBytes = pako.inflate(new Uint8Array(compressed))
  const xml = new TextDecoder('utf-8').decode(xmlBytes)
  const xmlParser = new DOMParser()
  const dom = xmlParser.parseFromString(xml, 'text/xml')
  return dom
}

export function availableSockets(pob: XMLDocument) {
  // This function executes the following psudocode on the PathObBuilding-root XML document:
  // equippedItemIds:= PathOfBuilding > Items > ItemSet[name is "Weapon 1" or "Weapon 2" or "Weapon 1 Swap" or "Weapon 2 Swap"][itemId]
  // equippedItems:= PathOfBuilding > Items > Item[id is in equippedItemIds].innerValue
  // availableSockets:= equippedItems.matches(/^Sockets:\s*((?:[RGBW]-?)*)/gmi)

  // get the first container
  const itemsContainers = pob.getElementsByTagName("Items")
  const itemsContainer = itemsContainers.item(0)
  if (!itemsContainer) {
    throw new Error("Missing Items node")
  }
  // determine active itemset
  const itemSetElements = pob.getElementsByTagName("ItemSet")
  const activeItemSetIndex = parseInt(itemsContainer.getAttribute("activeItemSet") ?? '')
  let activeItemSet
  for (let idx = 0; idx < itemSetElements.length; idx += 1) {
    const itemSet = itemSetElements.item(idx)
    if (!!itemSet && isParentOf(itemSet, itemsContainer) && parseInt(itemSet.getAttribute("id") ?? '') === activeItemSetIndex) {
      activeItemSet = itemSet
    }
  }
  if (!activeItemSet) {
    throw new Error(`No active ItemSet found with id = '${activeItemSetIndex}'`)
  }

  // determine all itemIds of slots in the active itemset
  const slotElements = pob.getElementsByTagName("Slot")
  const activeSlots = []
  for (let idx = 0; idx < slotElements.length; idx += 1) {
    const slot = slotElements.item(idx)
    if (!!slot && isParentOf(slot, activeItemSet)) {
      activeSlots.push(slot)
    }
  }
  const activeItemIds = new Set(
    activeSlots
      .map(x => x.getAttribute("itemId"))
      .filter(x => !!x)
      .map(x => parseInt(x ?? ''))
  )

  // determine items which are active
  const itemElements = pob.getElementsByTagName("Item")
  const activeItems = []
  for (let idx = 0; idx < itemElements.length; idx += 1) {
    const item = itemElements.item(idx)
    const itemid = item?.getAttribute("id")
    if (!!item && isParentOf(item, itemsContainer) && !!itemid && activeItemIds.has(parseInt(itemid))) {
      activeItems.push(item)
    }
  }

  // match all 'Sockets: X-X' expressions and aggregate the number of sockets
  const sockets = {
    red: 0,
    green: 0,
    blue: 0,
    white: 0,
  }

  for (const item of activeItems) {
    const match = /^Sockets:\s*?((?:[RGBW]-?)+)/gmi.exec(item.textContent ?? '')
    const group = match?.at(1)
    if (!!group) {
      if (item.textContent?.includes("Dialla's Malefaction")) {
        sockets.white += Math.ceil(group.length * 0.5)
        continue
      }
      sockets.red += [...group.matchAll(/R/gmi)].length
      sockets.green += [...group.matchAll(/G/gmi)].length
      sockets.blue += [...group.matchAll(/B/gmi)].length
      sockets.white += [...group.matchAll(/W/gmi)].length
    }
  }

  return sockets
}

function isParentOf(element: Element, parent: Element) {
  while (element !== parent) {
    if (!element.parentElement) {
      return false
    }
    element = element.parentElement
  }
  return true
}