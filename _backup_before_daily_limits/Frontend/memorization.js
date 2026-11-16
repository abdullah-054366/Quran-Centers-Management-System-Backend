import { api } from "./axiosClient";

const MEMORIZATION_ENDPOINT = "/Memorization";

export const getMemorizationRecords = async () => {
  try {
    const response = await api.get(`${MEMORIZATION_ENDPOINT}/All`);
    return response.data;
  } catch (error) {
    console.error("Error fetching memorization records:", error);
    throw error;
  }
};

export const getMemorizationStudents = async () => {
  try {
    const response = await api.get(`${MEMORIZATION_ENDPOINT}/Students`);
    return response.data;
  } catch (error) {
    console.error("Error fetching memorization students:", error);
    throw error;
  }
};

export const saveMemorizationRecords = async (records) => {
  try {
    const response = await api.post(`${MEMORIZATION_ENDPOINT}/Bulk`, records);
    return response.data;
  } catch (error) {
    console.error("Error saving memorization records:", error);
    throw error;
  }
};
