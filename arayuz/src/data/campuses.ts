export type CampusId = 'yesilyurt' | 'battalgazi'

export const campuses: {
  id: CampusId
  nameKey: 'home.yesilyurt' | 'home.battalgazi'
  addressKey: 'home.yesilyurtAddress' | 'home.battalgaziAddress'
  query: string
}[] = [
  {
    id: 'yesilyurt',
    nameKey: 'home.yesilyurt',
    addressKey: 'home.yesilyurtAddress',
    query: 'Malatya Turgut Özal Üniversitesi Yeşilyurt Yerleşkesi, İkizce Mahallesi İkizce Sokak No:100 Yeşilyurt Malatya',
  },
  {
    id: 'battalgazi',
    nameKey: 'home.battalgazi',
    addressKey: 'home.battalgaziAddress',
    query: 'Malatya Turgut Özal Üniversitesi Battalgazi Yerleşkesi, Boran Mahallesi Kırkgöz Caddesi No:82B Battalgazi Malatya',
  },
]
