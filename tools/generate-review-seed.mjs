import { mkdir, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const output = resolve(root, "backend/AirlineApi/SeedData/airline-reviews.json");

const airlines = [
  ["2U", "Sundor"], ["3F", "FLYONE ARMENIA LLC"], ["6H", "Israir Airlines"],
  ["A3", "Aegean Airlines"], ["A4", "AZIMUTH AIRLINES"], ["AC", "AIR CANADA"],
  ["AF", "AIR FRANCE"], ["AM", "AERO MEXICO"], ["AR", "AEROLINEAS ARGENTINAS S.A."],
  ["AZ", "ITALIA TRASPORTO AEREO"], ["B2", "BELAVIA AIRLINE"], ["BT", "AIR BALTIC"],
  ["BZ", "BLUE BIRD AIRWAYS"], ["C6", "CENTRUM AIR"], ["CY", "CYPRUS AIRWAYS"],
  ["DE", "CONDOR FLUGDIENST"], ["DL", "DELTA AIRLINES"], ["E2", "Air Haifa"],
  ["EK", "EMIRATES"], ["ENT", "ENTER AIR"], ["ET", "ETHIOPIAN AIRLINES"],
  ["EW", "EUROWINGS"], ["EY", "ETIHAD AIRWAYS"], ["FZ", "FLYDUBAI"],
  ["GQ", "SKY EXPRESS S.A."], ["H4", "HISKY EUROPE SRL"], ["H7", "HISKY"],
  ["HM", "AIR SEYCHELLES"], ["HY", "UZBEKISTAN AIR"], ["IB", "IBERIA"],
  ["IZ", "Arkia Israeli Airlines"], ["J2", "AZERBAIJAN AIRLINES"], ["JU", "AIR SERBIA"],
  ["KL", "K.L.M."], ["LH", "Lufthansa"], ["LO", "LOT POLISH AIRLINES"],
  ["LX", "SWISS INT'L AIRLINES"], ["LY", "El Al Israel Airlines"],
  ["MU", "CHINA EASTERN AIRLINES"], ["OS", "AUSTRIAN AIRLINES"], ["QF", "QANTAS AIRWAYS"],
  ["QS", "SMARTWINGS"], ["RO", "TAROM"], ["SK", "SAS"],
  ["SN", "BRUSSELS AIRLINES"], ["TG", "THAI AIRWAYS"], ["TO", "TRANSAVIA FRANCE"],
  ["TP", "AIR PORTUGAL"], ["U8", "TUS AIRWAYS"], ["UA", "UNITED AIRLINES"],
  ["UL", "SRILANKA AIRLINES"], ["UX", "AIR EUROPA"], ["VN", "VIETNAM AIRLINES"],
  ["VS", "VIRGIN ATLANTIC AIRWAYS"], ["W4", "WIZZ AIR MALTA"], ["W6", "Wizz Air"],
  ["W9", "WIZZ UK"], ["WZ", "RED WINGS AIRLINES"]
];

const templates = [
  [5, 4, "My {airline} flight from Tel Aviv was smooth, with friendly staff and a clean cabin."],
  [4, 9, "Boarding with {airline} was organized and the helpful gate staff explained every step clearly."],
  [4, 16, "The seat on {airline} was comfortable and the cabin crew provided good service throughout the flight."],
  [3, 24, "The {airline} flight was late, but the staff remained helpful and kept passengers informed."],
  [5, 31, "I had an excellent experience with {airline}; check-in was quick and the flight was very smooth."],
  [2, 38, "My {airline} flight had a long delay and baggage delivery was poor after landing."],
  [4, 47, "The {airline} aircraft was clean, the crew were friendly, and the arrival process was easy."],
  [3, 58, "Service on {airline} was good overall, although the seat felt tight on the longer sector."],
  [5, 67, "A great trip with {airline}: helpful staff, comfortable seating, and an on-time arrival."],
  [4, 81, "The digital check-in for {airline} worked well and boarding moved smoothly without confusion."],
  [2, 96, "The {airline} departure was cancelled after a delay, and support at the airport was poor."],
  [4, 118, "I would choose {airline} again because the cabin was clean and the service was friendly."],
];

const reviewsFor = (name, offset) => templates.map(([rating, daysAgo, text], index) => ({
  rating,
  daysAgo: daysAgo + ((offset + index) % 4),
  text: text.replaceAll("{airline}", name)
}));

const data = {
  description: "Static demonstration reviews used to seed the airline intelligence platform.",
  reviewsPerAirline: templates.length,
  defaultReviews: templates.map(([rating, daysAgo, text]) => ({ rating, daysAgo, text })),
  airlines: airlines.map(([iataCode, name], index) => ({
    iataCode,
    name,
    reviews: reviewsFor(name, index)
  }))
};

await mkdir(dirname(output), { recursive: true });
await writeFile(output, `${JSON.stringify(data, null, 2)}\n`, "utf8");
console.log(`Wrote ${airlines.length * templates.length} reviews to ${output}`);
