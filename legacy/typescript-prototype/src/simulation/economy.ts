import { BALANCE } from '../data/balance';
import type { CityState } from './city-state';
import { buildingUpkeep, jobsByCategory } from './services';

export type EconomySettlement = {
  income: number;
  expense: number;
  net: number;
};

export function settleEconomy(city: CityState): EconomySettlement {
  const buildings = city.getBuildings();
  const commercialJobs = jobsByCategory(buildings, 'commercial');
  const industrialJobs = jobsByCategory(buildings, 'industrial');
  const citizenIncome = city.metrics.population * BALANCE.baseTaxPerCitizen;
  const businessIncome =
    commercialJobs * BALANCE.commercialTaxPerJob + industrialJobs * BALANCE.industrialTaxPerJob;
  const income = Math.round((citizenIncome + businessIncome) * city.taxRate);
  const expense = Math.round(buildingUpkeep(buildings));
  const net = income - expense;

  city.metrics.cash += net;
  return { income, expense, net };
}
